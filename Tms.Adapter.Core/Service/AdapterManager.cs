using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Tms.Adapter.Core.Client;
using Tms.Adapter.Core.Models;
using Tms.Adapter.Core.Storage;
using Tms.Adapter.Core.Utils;
using Tms.Adapter.Core.Writer;
using LoggerFactory = Tms.Adapter.Core.Logger.LoggerFactory;

namespace Tms.Adapter.Core.Service;

public sealed class AdapterManager
{
    public static Func<string> CurrentTestIdGetter { get; } =
        () => Environment.CurrentManagedThreadId.ToString();

    private static readonly object Obj = new();
    private static AdapterManager? _instance;
    private readonly ResultStorage _storage;
    private readonly IWriter _writer;
    private readonly ITmsClient _client;
    private readonly ILogger<AdapterManager> _logger;
    private readonly ConcurrentDictionary<string, string> _messageByTestId = new();
    private readonly ConcurrentDictionary<string, List<Link>> _linksByTestId = new();

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
        _client = new TmsClient(logger.CreateLogger<TmsClient>(), config);
        _storage = new ResultStorage();
        _writer = new Writer.Writer(logger.CreateLogger<Writer.Writer>(), _client, config);
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

        if (links != null && links.Count > 0 )
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
        _writer.Write(_storage.Remove<TestContainer>(uuid),
            _storage.Remove<ClassContainer>(containerId)).Wait();
        return this;
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
        var list = _linksByTestId.GetOrAdd(CurrentTestIdGetter(), _ => new List<Link>());
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

    public static void ClearInstance()
    {
        _instance = null;
    }
}