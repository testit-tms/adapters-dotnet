using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Globalization;
using Tms.Adapter.Core.Client;
using Tms.Adapter.Core.Models;
using Tms.Adapter.Core.Storage;
using Tms.Adapter.Core.SyncStorage;
using Tms.Adapter.Core.Utils;
using LoggerFactory = Tms.Adapter.Core.Logger.LoggerFactory;

namespace Tms.Adapter.Core.Service;

public sealed class AdapterManager : IDisposable
{
    private const string InProgressLiteral = "InProgress";
    private const string DisableNetworkEnvVar = "TMS_DISABLE_NETWORK";

    public static Func<string> CurrentTestIdGetter { get; } =
        () => Environment.CurrentManagedThreadId.ToString(CultureInfo.InvariantCulture);

    private static readonly object Obj = new();
    private static AdapterManager? _instance;
    private readonly ResultStorage _storage;
    private readonly Writer.Writer _writer;
    private readonly ITmsClient _client;
    private readonly ILogger<AdapterManager> _logger;
    private readonly ConcurrentDictionary<string, string> _messageByTestId = new();
    private readonly ConcurrentDictionary<string, List<Link>> _linksByTestId = new();
    private readonly object _writeLock = new();

    private SyncStorageRunner? _syncStorageRunner;

    public static AdapterManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (Obj)
                {
                    if (_instance == null)
                    {
                        _instance = new AdapterManager();
                    }
                }
            }

            return _instance;
        }
    }

    public AdapterManager()
    {
        var config = Configurator.Configurator.GetConfig();
        var logger = LoggerFactory.GetLogger(config.IsDebug);
        _logger = logger.CreateLogger<AdapterManager>();
        _client = IsNetworkDisabled()
            ? new NoopTmsClient()
            : new TmsClient(logger.CreateLogger<TmsClient>(), config);
        _storage = new ResultStorage();
        _writer = new Writer.Writer(logger.CreateLogger<Writer.Writer>(), _client, config);

        // Initialize Sync Storage
        if (!IsNetworkDisabled())
        {
            InitializeSyncStorage(config, logger);
        }
    }

    private static bool IsNetworkDisabled()
    {
        return string.Equals(Environment.GetEnvironmentVariable(DisableNetworkEnvVar), "true",
            StringComparison.OrdinalIgnoreCase);
    }

    private void InitializeSyncStorage(Configurator.TmsSettings config, ILoggerFactory loggerFactory)
    {
        try
        {
            var testRunId = config.TestRunId;

            // If no test run ID, create one (needed for sync storage registration)
            if (string.IsNullOrEmpty(testRunId))
            {
                _client.CreateTestRun().Wait();
                testRunId = config.TestRunId;
            }

            if (string.IsNullOrEmpty(testRunId))
            {
                _logger.LogWarning("Cannot initialize SyncStorage: no test run ID");
                return;
            }

            _syncStorageRunner = new SyncStorageRunner(
                testRunId: testRunId,
                port: config.SyncStoragePort,
                baseUrl: config.Url,
                privateToken: config.PrivateToken,
                logger: loggerFactory.CreateLogger<SyncStorageRunner>());

            var started = _syncStorageRunner.StartAsync().GetAwaiter().GetResult();

            if (started)
            {
                _logger.LogInformation("SyncStorage initialized successfully");
            }
            else
            {
                _logger.LogWarning("Failed to start SyncStorage, continuing without it");
                _syncStorageRunner.Dispose();
                _syncStorageRunner = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize SyncStorage");
            _syncStorageRunner?.Dispose();
            _syncStorageRunner = null;
        }
    }

    public AdapterManager StartTestContainer(ClassContainer container)
    {
        container.Start = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        _logger.LogDebug("Starting class container: {@Container}", container);

        _storage.Put(container.Id, container);

        return this;
    }

    public AdapterManager StartTestContainer(string parentUuid, ClassContainer container)
    {
        UpdateTestContainer(parentUuid, c => c.Children.Add(container.Id));
        StartTestContainer(container);
        return this;
    }

    public AdapterManager UpdateTestContainer(string uuid, Action<ClassContainer> update)
    {
        _logger.LogDebug("Updating class container with id: {ID}", uuid);

        // SpecFlow (and some runners) can reference a "root" container id (e.g. feature hash)
        // that wasn't explicitly started. Ensure it exists to avoid KeyNotFoundException.
        _storage.Put(uuid, new ClassContainer { Id = uuid });
        update.Invoke(_storage.Get<ClassContainer>(uuid));
        return this;
    }

    public AdapterManager StopTestContainer(string uuid)
    {
        _logger.LogDebug("Stopping class container with id: {ID}", uuid);

        UpdateTestContainer(uuid, c => c.Stop = DateTimeOffset.Now.ToUnixTimeMilliseconds());
        return this;
    }

    public AdapterManager StartBeforeFixture(string parentUuid, FixtureResult result)
    {
        var uuid = Hash.NewId();
        StartBeforeFixture(parentUuid, uuid, result);
        return this;
    }

    public AdapterManager StartBeforeFixture(string parentUuid, string uuid, FixtureResult result)
    {
        UpdateTestContainer(parentUuid, container => container.Befores.Add(result));
        StartFixture(uuid, result);
        return this;
    }

    public AdapterManager StartAfterFixture(string parentUuid, FixtureResult result)
    {
        var uuid = Guid.NewGuid().ToString("N");
        StartAfterFixture(parentUuid, uuid, result);
        return this;
    }

    public AdapterManager StartAfterFixture(string parentUuid, string uuid, FixtureResult result)
    {
        UpdateTestContainer(parentUuid, container => container.Afters.Add(result));
        StartFixture(uuid, result);
        return this;
    }

    private void StartFixture(string uuid, FixtureResult fixtureResult)
    {
        _storage.Put(uuid, fixtureResult);
        fixtureResult.Stage = Stage.Running;
        fixtureResult.Start = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        _logger.LogDebug("Starting fixture with id {ID}: {@Fixture}", uuid, fixtureResult);

        _storage.ClearStepContext();
        _storage.StartStep(uuid);
    }

    public AdapterManager UpdateFixture(Action<FixtureResult> update)
    {
        UpdateFixture(_storage.GetRootStep()!, update);
        return this;
    }

    public AdapterManager UpdateFixture(string uuid, Action<FixtureResult> update)
    {
        update.Invoke(_storage.Get<FixtureResult>(uuid));
        return this;
    }

    public AdapterManager StopFixture(Action<FixtureResult> beforeStop)
    {
        UpdateFixture(beforeStop);
        return StopFixture(_storage.GetRootStep()!);
    }

    public AdapterManager StopFixture(string uuid)
    {
        var fixture = _storage.Remove<FixtureResult>(uuid);
        _storage.ClearStepContext();
        fixture.Stage = Stage.Finished;
        fixture.Stop = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        _logger.LogDebug("Stopping fixture with id {ID}: {@Fixture}", uuid, fixture);

        return this;
    }

    public AdapterManager StartTestCase(string containerUuid, TestContainer testResult)
    {
        UpdateTestContainer(containerUuid, c => c.Children.Add(testResult.Id));
        return StartTestCase(testResult);
    }

    public AdapterManager StartTestCase(TestContainer testResult)
    {
        testResult.Stage = Stage.Running;
        testResult.Start = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        _logger.LogDebug("Starting test container: {@Container}", testResult);

        _storage.Put(testResult.Id, testResult);
        _storage.ClearStepContext();
        _storage.StartStep(testResult.Id);
        return this;
    }

    public AdapterManager UpdateTestCase(string uuid, Action<TestContainer> update)
    {
        update.Invoke(_storage.Get<TestContainer>(uuid));
        return this;
    }

    public AdapterManager UpdateTestCase(Action<TestContainer> update)
    {
        return UpdateTestCase(_storage.GetRootStep()!, update);
    }

    public AdapterManager StopTestCase(Action<TestContainer> beforeStop)
    {
        UpdateTestCase(beforeStop);
        return StopTestCase(_storage.GetRootStep()!);
    }

    public AdapterManager StopTestCase(string uuid)
    {
        var testResult = _storage.Get<TestContainer>(uuid);

        _messageByTestId.TryRemove(CurrentTestIdGetter(), out var message);

        if (!string.IsNullOrEmpty(message) && testResult.Status != Status.Failed)
        {
            testResult.Message = message;
        }

        _linksByTestId.TryRemove(CurrentTestIdGetter(), out var links);

        if (links != null && links.Count > 0)
        {
            lock (links)
            {
                testResult.ResultLinks.AddRange(links);
            }
        }

        testResult.Stage = Stage.Finished;
        testResult.Stop = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        _logger.LogDebug("Stopping test container: {@Container}", testResult);

        _storage.ClearStepContext();
        return this;
    }

    public AdapterManager WriteTestCase(string uuid, string containerId)
    {
        lock (_writeLock)
        {
            var testContainer = _storage.Remove<TestContainer>(uuid);
            var classContainer = _storage.Remove<ClassContainer>(containerId);

            if (testContainer == null || classContainer == null)
            {
                _logger.LogDebug("Skip write: test or container not found (testId={TestId}, containerId={ContainerId})",
                    uuid, containerId);
                return this;
            }

            // Serialize writes so the in-progress master result is always flushed first.
            // This prevents parallel test threads from writing other results ahead of it.
            if (IsSyncStorageActive() && IsMasterAndNoInProgress())
            {
                if (TrySendToSyncStorageAndWriteInProgress(testContainer, classContainer))
                {
                    return this;
                }
                // Fall through to normal write on failure
            }

            _writer.Write(testContainer, classContainer).Wait();
            return this;
        }
    }

    /// <summary>
    /// Sends in-progress preview to Sync Storage (master slot), publishes InProgress to TMS, then the final outcome.
    /// </summary>
    private bool TrySendToSyncStorageAndWriteInProgress(TestContainer testContainer, ClassContainer classContainer)
    {
        try
        {
            var cfg = Tms.Adapter.Core.Configurator.Configurator.GetConfig();
            if (string.IsNullOrWhiteSpace(cfg.ProjectId))
            {
                _logger.LogWarning("Sync Storage in-progress skipped: ProjectId is not configured.");
                return false;
            }

            var cutModel = Converter.ToTestResultCutApiModel(testContainer, cfg.ProjectId);

            _logger.LogDebug(
                "Sending to SyncStorage: ExternalId={ExternalId}, Status={Status}",
                cutModel.AutoTestExternalId, cutModel.StatusCode);

            var success = _syncStorageRunner!.SendInProgressTestResultAsync(cutModel)
                .GetAwaiter().GetResult();

            if (!success)
            {
                return false;
            }

            var originalStatus = testContainer.Status;
            testContainer.Status = Status.InProgress;

            try
            {
                _writer.Write(testContainer, classContainer).Wait();
                testContainer.Status = originalStatus;
                _writer.Write(testContainer, classContainer).Wait();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write SyncStorage-backed test results to TMS, falling back");
                testContainer.Status = originalStatus;
                return false;
            }
            finally
            {
                _syncStorageRunner!.SetIsAlreadyInProgress(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SyncStorage handling failed, falling back to normal write");
            return false;
        }
    }

    public AdapterManager StartStep(StepResult result)
    {
        var uuid = Hash.NewId();
        StartStep(_storage.GetCurrentStep()!, uuid, result);
        return this;
    }

    public AdapterManager StartStep(string uuid, StepResult result)
    {
        StartStep(_storage.GetCurrentStep()!, uuid, result);
        return this;
    }

    public AdapterManager StartStep(string parentUuid, string uuid, StepResult stepResult)
    {
        stepResult.Stage = Stage.Running;
        stepResult.Start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        _storage.StartStep(uuid);
        _storage.AddStep(parentUuid, uuid, stepResult);

        _logger.LogDebug("Starting step: {@Step}", stepResult);

        return this;
    }

    public AdapterManager UpdateStep(Action<StepResult> update)
    {
        update.Invoke(_storage.Get<StepResult>(_storage.GetCurrentStep()!));
        return this;
    }

    public AdapterManager UpdateStep(string uuid, Action<StepResult> update)
    {
        update.Invoke(_storage.Get<StepResult>(uuid));
        return this;
    }

    public AdapterManager StopStep(Action<StepResult> beforeStop)
    {
        UpdateStep(beforeStop);
        return StopStep(_storage.GetCurrentStep()!);
    }

    public AdapterManager StopStep(string uuid)
    {
        var step = _storage.Remove<StepResult>(uuid);
        step.Stage = Stage.Finished;
        step.Stop = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        _storage.StopStep();

        _logger.LogDebug("Stopping step with id {ID}: {@Step}", uuid, step);

        return this;
    }

    public AdapterManager AddMessage(string message)
    {
        _messageByTestId[CurrentTestIdGetter()] = message;
        return this;
    }

    public AdapterManager AddLinks(IEnumerable<Link> links)
    {
        var list = _linksByTestId.GetOrAdd(CurrentTestIdGetter(), _ => []);
        lock (list)
        {
            list.AddRange(links);
        }
        return this;
    }

    public AdapterManager AddAttachments(string filename, Stream content)
    {
        var attachId = _client.UploadAttachment(filename, content).Result;
        _storage.Get<ExecutableItem>(_storage.GetCurrentStep()!).Attachments.Add(attachId);
        return this;
    }

    public async Task CreateTestRun()
    {
        await _client.CreateTestRun().ConfigureAwait(false);
    }

    public async Task UpdateTestRun()
    {
        await _client.UpdateTestRun().ConfigureAwait(false);
    }

    public async Task CompleteTestRun()
    {
        await _client.CompleteTestRun().ConfigureAwait(false);
    }

    /// <summary>
    /// Notify Sync Storage that test running has started (sets worker status to in_progress).
    /// Call at the beginning of a test run.
    /// </summary>
    public void OnRunningStarted()
    {
        SetWorkerStatus("in_progress");
    }

    /// <summary>
    /// Notify Sync Storage that the current block has completed (sets worker status to completed).
    /// Call at the end of a test session/block.
    /// </summary>
    public void OnBlockCompleted()
    {
        SetWorkerStatus("completed");
    }

    private void SetWorkerStatus(string status)
    {
        if (!IsSyncStorageActive())
        {
            return;
        }

        _logger.LogInformation("Setting worker status to {Status}", status);

        try
        {
            _syncStorageRunner!.SetWorkerStatusAsync(status).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set worker status to {Status}", status);
        }
    }

    private bool IsSyncStorageActive()
    {
        return _syncStorageRunner != null && _syncStorageRunner.IsRunning;
    }

    private bool IsMasterAndNoInProgress()
    {
        return _syncStorageRunner!.IsMaster && !_syncStorageRunner.IsAlreadyInProgress;
    }

    public static void ClearInstance()
    {
        _instance = null;
    }

    public void Dispose()
    {
        _syncStorageRunner?.Dispose();
        if (_client is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
