using Tms.Adapter.Core.Models;
using Tms.Adapter.Core.Service;

namespace Tms.Adapter.XUnit;

public static class StepManager
{
    private static readonly AsyncLocal<ITmsAccessor> TmsAccessor = new();

    internal static ITmsAccessor TestResultAccessor
    {
        get => TmsAccessor.Value;
        set => TmsAccessor.Value = value;
    }

    public static void StartBeforeFixture(string name)
    {
        var fixtureResult = new FixtureResult
        {
            DisplayName = name,
            Stage = Stage.Running,
            Start = DateTimeOffset.Now.ToUnixTimeMilliseconds()
        };

        AdapterManager.Instance.StartBeforeFixture(TestResultAccessor.ClassContainer.Id, fixtureResult);
    }

    public static void StartAfterFixture(string name)
    {
        var fixtureResult = new FixtureResult
        {
            DisplayName = name,
            Stage = Stage.Running,
            Start = DateTimeOffset.Now.ToUnixTimeMilliseconds()
        };

        AdapterManager.Instance.StartAfterFixture(TestResultAccessor.ClassContainer.Id, fixtureResult);
    }

    public static void StopFixture(Action<FixtureResult> updateResults = null)
    {
        AdapterManager.Instance.StopFixture(result =>
        {
            result.Stage = Stage.Finished;
            result.Stop = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            updateResults?.Invoke(result);
        });
    }

    public static void StopFixtureSuppressTestCase(Action<FixtureResult> updateResults = null)
    {
        var newTestResult = TestResultAccessor.TestResult;
        StopFixture(updateResults);
        AdapterManager.Instance.StartTestCase(TestResultAccessor.ClassContainer.Id, newTestResult);
    }

    public static void StartStep(string name, Action<StepResult> updateResults = null)
    {
        var stepResult = new StepResult
        {
            DisplayName = name,
            Stage = Stage.Running,
            Start = DateTimeOffset.Now.ToUnixTimeMilliseconds()
        };
        updateResults?.Invoke(stepResult);

        AdapterManager.Instance.StartStep(stepResult);
    }

    public static void PassStep(Action<StepResult> updateResults = null)
    {
        AdapterManager.Instance.StopStep(result =>
        {
            result.Status = Status.Passed;
            updateResults?.Invoke(result);
        });
    }

    public static void FailStep()
    {
        AdapterManager.Instance.StopStep(result => { result.Status = Status.Failed; });
    }
}