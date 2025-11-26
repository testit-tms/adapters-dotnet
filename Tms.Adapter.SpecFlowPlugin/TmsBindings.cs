using TechTalk.SpecFlow;
using Tms.Adapter.Core.Models;
using Tms.Adapter.Core.Service;

namespace Tms.Adapter.SpecFlowPlugin;

[Binding]
public class TmsBindings
{
    private static readonly AdapterManager Adapter = AdapterManager.Instance;

    private readonly FeatureContext _featureContext;
    private readonly ScenarioContext _scenarioContext;

    public TmsBindings(FeatureContext featureContext, ScenarioContext scenarioContext)
    {
        _featureContext = featureContext;
        _scenarioContext = scenarioContext;
    }

    [BeforeTestRun]
    public static void BeforeTestRun()
    {
        Adapter.UpdateTestRun().Wait();
        Adapter.CreateTestRun().Wait();
    }

    [AfterTestRun]
    public static void AfterTestRun()
    {
        Adapter.CompleteTestRun().Wait();
    }

    [BeforeFeature(Order = int.MinValue)]
    public static void FirstBeforeFeature()
    {
    }

    [AfterFeature(Order = int.MaxValue)]
    public static void LastAfterFeature()
    {
    }

    [BeforeScenario(Order = int.MinValue)]
    public void FirstBeforeScenario()
    {
        TmsHelper.StartTestContainer(_featureContext, _scenarioContext);
    }

    [BeforeScenario(Order = int.MaxValue)]
    public void LastBeforeScenario()
    {
        var scenarioContainer = TmsHelper.GetCurrentTestContainer(_scenarioContext);
        TmsHelper.StartTestCase(scenarioContainer.Id, _featureContext, _scenarioContext);
    }

    [AfterScenario(Order = int.MinValue)]
    public void FirstAfterScenario()
    {
        var scenarioId = TmsHelper.GetCurrentTestCase(_scenarioContext).Id;

        Adapter
            .UpdateTestCase(scenarioId,
                x => x.Status = x.Status == Status.Undefined ? Status.Passed : x.Status)
            .StopTestCase(scenarioId);
    }
}