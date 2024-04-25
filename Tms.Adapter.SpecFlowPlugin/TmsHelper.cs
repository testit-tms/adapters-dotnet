using System.Collections.Specialized;
using System.Globalization;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using Tms.Adapter.Core.Models;
using Tms.Adapter.Core.Service;
using Tms.Adapter.Core.Utils;

namespace Tms.Adapter.SpecFlowPlugin;

public static class TmsHelper
{
    private static readonly FeatureInfo EmptyFeatureInfo = new FeatureInfo(
        CultureInfo.CurrentCulture, string.Empty, string.Empty, string.Empty);

    private static readonly ScenarioInfo EmptyScenarioInfo =
        new ScenarioInfo("Unknown", string.Empty, Array.Empty<string>(), new OrderedDictionary());
        
    internal static FixtureResult GetFixtureResult(HookBinding hook)
    {
        return new FixtureResult
        {
            DisplayName = $"{hook.Method.Name} [{hook.HookOrder}]"
        };
    }

    internal static string GetFeatureContainerId(FeatureInfo featureInfo)
    {
        var id = featureInfo != null
            ? featureInfo.GetHashCode().ToString()
            : EmptyFeatureInfo.GetHashCode().ToString();

        return id;
    }

    internal static ClassContainer StartTestContainer(FeatureContext featureContext,
        ScenarioContext scenarioContext)
    {
        var containerId = GetFeatureContainerId(featureContext?.FeatureInfo);

        var scenarioContainer = new ClassContainer
        {
            Id = Hash.NewId()
        };
        AdapterManager.Instance.StartTestContainer(containerId, scenarioContainer);
        scenarioContext?.Set(scenarioContainer);
        featureContext?.Get<HashSet<ClassContainer>>().Add(scenarioContainer);

        return scenarioContainer;
    }

    internal static TestContainer StartTestCase(string containerId, FeatureContext featureContext,
        ScenarioContext scenarioContext)
    {
        var featureInfo = featureContext?.FeatureInfo ?? EmptyFeatureInfo;
        var scenarioInfo = scenarioContext?.ScenarioInfo ?? EmptyScenarioInfo;

        var parameters = GetParameters(scenarioInfo);
        var testResult = new TestContainer
        {
            Id = Hash.NewId(),
            Parameters = parameters
        };
        testResult = TmsTagParser.AddTags(testResult, featureInfo, scenarioInfo, parameters);
            
        AdapterManager.Instance.StartTestCase(containerId, testResult);
        scenarioContext?.Set(testResult);
        featureContext?.Get<HashSet<TestContainer>>().Add(testResult);

        return testResult;
    }

    private static Dictionary<string, string> GetParameters(ScenarioInfo scenarioInfo)
    {
        var parameters = new Dictionary<string, string>();
        var argumentsEnumerator = scenarioInfo.Arguments.GetEnumerator();
        while (argumentsEnumerator.MoveNext())
        {
            parameters.Add(key: argumentsEnumerator.Key.ToString(), value: argumentsEnumerator.Value.ToString());
        }

        return parameters;
    }

    internal static TestContainer GetCurrentTestCase(ScenarioContext context)
    {
        context.TryGetValue(out TestContainer testContainer);
        return testContainer;
    }

    internal static ClassContainer GetCurrentTestContainer(ScenarioContext context)
    {
        context.TryGetValue(out ClassContainer classContainer);
        return classContainer;
    }
}