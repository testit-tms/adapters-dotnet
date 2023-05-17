using Microsoft.Extensions.Logging;
using Tms.Adapter.Core.Client;
using Tms.Adapter.Core.Models;
using Tms.Adapter.Core.Storage;
using Tms.Adapter.Core.Writer;
using LoggerFactory = Tms.Adapter.Core.Logger.LoggerFactory;

namespace Tms.Adapter.Core.Service;

public class AdapterManager
{
    public static Func<string> CurrentTestIdGetter { get; set; } =
        () => Thread.CurrentThread.ManagedThreadId.ToString();

    private static readonly object Obj = new();
    private static AdapterManager _instance;
    private readonly ResultStorage _storage;
    private readonly IWriter _writer;
    private readonly ITmsClient _client;
    private bool _isCreateTestRun = false;

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
        var logger = LoggerFactory.GetLogger(true);
        _client = new TmsClient(logger.CreateLogger<TmsClient>(), Configurator.Configurator.GetConfig());
        _storage = new ResultStorage();
        _writer = new Writer.Writer(logger.CreateLogger<Writer.Writer>(), _client);
    }
    
    public virtual AdapterManager StartTestContainer(TestResultContainer container)
    {
        if (!_isCreateTestRun)
        {
            _client.CreatTestRun().Wait();
            _isCreateTestRun = true;
        }
        
        container.Start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        _storage.Put(container.Id, container);
        
        return this;
    }

    public virtual AdapterManager StartTestContainer(string parentUuid, TestResultContainer container)
    {
        UpdateTestContainer(parentUuid, c => c.Children.Add(container.Id));
        StartTestContainer(container);
        
        return this;
    }

    public virtual AdapterManager UpdateTestContainer(string uuid, Action<TestResultContainer> update)
    {
        update.Invoke(_storage.Get<TestResultContainer>(uuid));
        return this;
    }

    public virtual AdapterManager StopTestContainer(string uuid)
    {
        UpdateTestContainer(uuid, c => c.Stop = DateTimeOffset.Now.ToUnixTimeMilliseconds());
        return this;
    }

    public virtual AdapterManager WriteTestContainer(string uuid)
    {
        _writer.Write(_storage.Remove<TestResultContainer>(uuid));
        return this;
    }

    public virtual AdapterManager StartBeforeFixture(string parentUuid, FixtureResult result, out string uuid)
    {
        uuid = Guid.NewGuid().ToString("N");
        StartBeforeFixture(parentUuid, uuid, result);
        return this;
    }

    public virtual AdapterManager StartBeforeFixture(string parentUuid, string uuid, FixtureResult result)
    {
        UpdateTestContainer(parentUuid, container => container.Befores.Add(result));
        StartFixture(uuid, result);
        return this;
    }

    public virtual AdapterManager StartAfterFixture(string parentUuid, FixtureResult result, out string uuid)
    {
        uuid = Guid.NewGuid().ToString("N");
        StartAfterFixture(parentUuid, uuid, result);
        return this;
    }

    public virtual AdapterManager StartAfterFixture(string parentUuid, string uuid, FixtureResult result)
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
        _storage.ClearStepContext();
        _storage.StartStep(uuid);
    }

    public virtual AdapterManager UpdateFixture(Action<FixtureResult> update)
    {
        UpdateFixture(_storage.GetRootStep(), update);
        return this;
    }

    public virtual AdapterManager UpdateFixture(string uuid, Action<FixtureResult> update)
    {
        update.Invoke(_storage.Get<FixtureResult>(uuid));
        return this;
    }

    public virtual AdapterManager StopFixture(Action<FixtureResult> beforeStop)
    {
        UpdateFixture(beforeStop);
        return StopFixture(_storage.GetRootStep());
    }

    public virtual AdapterManager StopFixture(string uuid)
    {
        var fixture = _storage.Remove<FixtureResult>(uuid);
        _storage.ClearStepContext();
        fixture.Stage = Stage.Finished;
        fixture.Stop = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        return this;
    }
    
    public virtual AdapterManager StartTestCase(string containerUuid, TestResult testResult)
    {
        UpdateTestContainer(containerUuid, c => c.Children.Add(testResult.Id));
        return StartTestCase(testResult);
    }

    public virtual AdapterManager StartTestCase(TestResult testResult)
    {
        testResult.Stage = Stage.Running;
        testResult.Start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        _storage.Put(testResult.Id, testResult);
        _storage.ClearStepContext();
        _storage.StartStep(testResult.Id);
        return this;
    }

    public virtual AdapterManager UpdateTestCase(string uuid, Action<TestResult> update)
    {
        update.Invoke(_storage.Get<TestResult>(uuid));
        return this;
    }

    public virtual AdapterManager UpdateTestCase(Action<TestResult> update)
    {
        return UpdateTestCase(_storage.GetRootStep(), update);
    }

    public virtual AdapterManager StopTestCase(Action<TestResult> beforeStop)
    {
        UpdateTestCase(beforeStop);
        return StopTestCase(_storage.GetRootStep());
    }

    public virtual AdapterManager StopTestCase(string uuid)
    {
        var testResult = _storage.Get<TestResult>(uuid);
        testResult.Stage = Stage.Finished;
        testResult.Stop = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        _storage.ClearStepContext();
        return this;
    }

    public virtual AdapterManager WriteTestCase(string uuid, string containerId)
    {
        _writer.Write(_storage.Remove<TestResult>(uuid),
            _storage.Remove<TestResultContainer>(containerId)).Wait();
        return this;
    }
    
    public virtual AdapterManager StartStep(StepResult result, out string uuid)
    {
        uuid = Guid.NewGuid().ToString("N");
        StartStep(_storage.GetCurrentStep(), uuid, result);
        return this;
    }
        
    public virtual AdapterManager StartStep(string uuid, StepResult result)
    {
        StartStep(_storage.GetCurrentStep(), uuid, result);
        return this;
    }

    public virtual AdapterManager StartStep(string parentUuid, string uuid, StepResult stepResult)
    {
        stepResult.Stage = Stage.Running;
        stepResult.Start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        _storage.StartStep(uuid);
        _storage.AddStep(parentUuid, uuid, stepResult);
        return this;
    }

    public virtual AdapterManager UpdateStep(Action<StepResult> update)
    {
        update.Invoke(_storage.Get<StepResult>(_storage.GetCurrentStep()));
        return this;
    }

    public virtual AdapterManager UpdateStep(string uuid, Action<StepResult> update)
    {
        update.Invoke(_storage.Get<StepResult>(uuid));
        return this;
    }

    public virtual AdapterManager StopStep(Action<StepResult> beforeStop)
    {
        UpdateStep(beforeStop);
        return StopStep(_storage.GetCurrentStep());
    }

    public virtual AdapterManager StopStep(string uuid)
    {
        var step = _storage.Remove<StepResult>(uuid);
        step.Stage = Stage.Finished;
        step.Stop = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        _storage.StopStep();
        return this;
    }

    public virtual AdapterManager StopStep()
    {
        StopStep(_storage.GetCurrentStep());
        return this;
    }


}