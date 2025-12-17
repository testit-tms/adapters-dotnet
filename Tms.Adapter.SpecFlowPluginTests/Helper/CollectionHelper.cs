using Tms.Adapter.SpecFlowPlugin;
using TechTalk.SpecFlow.Configuration;
using BoDi;
using System.Globalization;
using TechTalk.SpecFlow.Infrastructure;
using TechTalk.SpecFlow;
using System.Collections.Specialized;
using TechTalk.SpecFlow.Bindings;
using System.Reflection;
using TechTalk.SpecFlow.Bindings.Reflection;
using TechTalk.SpecFlow.Tracing;

namespace Tms.Adapter.SpecFlowPluginTests.Helper;

public static class CollectionHelper
{
    public static ContextManager GetContextManager(SpecFlowConfiguration specFlowConfiguration, 
        TmsTestTracer testTracer, string[]? tags = null)
    {
        var featureInfo = new FeatureInfo(CultureInfo
            .GetCultureInfo("en-US"), "Features", "StepsTests", null);
        var scenarioInfo = new ScenarioInfo("", null, tags, new OrderedDictionary());

        var testThreadContainer = new ObjectContainer();
        var containerBuilder = new ContainerBuilder();

        testThreadContainer.RegisterInstanceAs(specFlowConfiguration);
        testThreadContainer.RegisterTypeAs(typeof(TestObjectResolver), typeof(ITestObjectResolver));

        var contextManager = new ContextManager(testTracer, testThreadContainer, containerBuilder);

        contextManager.InitializeFeatureContext(featureInfo);
        contextManager.InitializeScenarioContext(scenarioInfo);

        return contextManager;
    }

    public static HookBinding GetHookBinding(MethodInfo? methodInfo, HookType hookType, int hookOrder = 10000)
    {
        var bindingMethod = new RuntimeBindingMethod(methodInfo);
        return new HookBinding(bindingMethod, hookType, null, hookOrder);
    }

    private static HookBinding GetHookBinding(StatusBinding status)
    {
        switch (status)
        {
            case StatusBinding.FirstBeforeFeature:
                return GetHookBinding(typeof(TmsBindings).GetMethod("FirstBeforeFeature"), HookType.BeforeFeature, int.MinValue);

            case StatusBinding.FirstBeforeScenario:
                return GetHookBinding(typeof(TmsBindings).GetMethod("FirstBeforeScenario"), HookType.BeforeScenario, int.MinValue);

            case StatusBinding.LastBeforeScenario:
                return GetHookBinding(typeof(TmsBindings).GetMethod("LastBeforeScenario"), HookType.BeforeScenario, int.MaxValue);

            case StatusBinding.FirstAfterScenario:
                return GetHookBinding(typeof(TmsBindings).GetMethod("FirstAfterScenario"), HookType.AfterScenario, int.MinValue);

            case StatusBinding.LastAfterFeature:
                return GetHookBinding(typeof(TmsBindings).GetMethod("LastAfterFeature"), HookType.AfterFeature, int.MaxValue);

            default:
                throw new InvalidDataException($"Invalid status of type Hook");
        }
    }

    public static void InvokeBindingHelper(this IBindingInvoker bindingInvoker, StatusBinding statusBinding, ITestTracer testTracer, IContextManager contextManager)
    {
        var binding = GetHookBinding(statusBinding);

        bindingInvoker.InvokeBinding(binding, contextManager, null, testTracer, out TimeSpan _);
    }
}

