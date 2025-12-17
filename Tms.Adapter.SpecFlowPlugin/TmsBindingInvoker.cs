using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.ErrorHandling;
using TechTalk.SpecFlow.Infrastructure;
using TechTalk.SpecFlow.Tracing;
using Tms.Adapter.Core.Models;
using Tms.Adapter.Core.Service;
using Tms.Adapter.Core.Utils;

namespace Tms.Adapter.SpecFlowPlugin;

public class TmsBindingInvoker : BindingInvoker
{
    private static readonly AdapterManager Adapter = AdapterManager.Instance;

    public TmsBindingInvoker(SpecFlowConfiguration specFlowConfiguration, IErrorProvider errorProvider,
        ISynchronousBindingDelegateInvoker synchronousBindingDelegateInvoker) : base(
        specFlowConfiguration, errorProvider, synchronousBindingDelegateInvoker)
    {
    }

    public override object InvokeBinding(IBinding binding, IContextManager contextManager, object[] arguments,
        ITestTracer testTracer,
        out TimeSpan duration)
    {
        if (binding is not HookBinding hook)
            return base.InvokeBinding(binding, contextManager, arguments, testTracer, out duration);

        var featureContainerId = TmsHelper.GetFeatureContainerId(contextManager.FeatureContext?.FeatureInfo!);

        switch (hook.HookType)
        {
            case HookType.BeforeFeature:
                if (hook.HookOrder == int.MinValue)
                {
                    var featureContainer = new ClassContainer
                    {
                        Id = TmsHelper.GetFeatureContainerId(contextManager.FeatureContext?.FeatureInfo!)
                    };
                    Adapter.StartTestContainer(featureContainer);

                    contextManager.FeatureContext.Set(new HashSet<ClassContainer>());
                    contextManager.FeatureContext.Set(new HashSet<TestContainer>());

                    return base.InvokeBinding(binding, contextManager, arguments, testTracer, out duration);
                }

                try
                {
                    StartFixture(hook, featureContainerId);
                    var result = base.InvokeBinding(binding, contextManager, arguments, testTracer,
                        out duration);
                    Adapter.StopFixture(x => x.Status = Status.Passed);
                    return result;
                }
                catch (Exception ex)
                {
                    Adapter.StopFixture(x => x.Status = Status.Failed);

                    var scenarioContainer =
                        TmsHelper.StartTestContainer(contextManager.FeatureContext!, null!);

                    var scenario = TmsHelper.StartTestCase(scenarioContainer.Id,
                        contextManager.FeatureContext!, null!);

                    Adapter
                        .StopTestCase(x =>
                        {
                            x.Status = Status.Failed;
                            x.Message = ex.Message;
                            x.Trace = ex.StackTrace;
                        })
                        .StopTestContainer(scenarioContainer.Id)
                        .WriteTestCase(scenario.Id, scenarioContainer.Id);

                    throw;
                }

            case HookType.BeforeStep:
            case HookType.AfterStep:
            {
                var scenario = TmsHelper.GetCurrentTestCase(contextManager.ScenarioContext);

                try
                {
                    return base.InvokeBinding(binding, contextManager, arguments, testTracer, out duration);
                }
                catch (Exception ex)
                {
                    Adapter
                        .UpdateTestCase(scenario.Id,
                            x =>
                            {
                                x.Status = Status.Failed;
                                x.Message = ex.Message;
                                x.Trace = ex.StackTrace;
                            });
                    throw;
                }
            }

            case HookType.BeforeScenario:
            case HookType.AfterScenario:
                if (hook.HookOrder == int.MinValue || hook.HookOrder == int.MaxValue)
                {
                    return base.InvokeBinding(binding, contextManager, arguments, testTracer, out duration);
                }

            {
                var scenarioContainer = TmsHelper.GetCurrentTestContainer(contextManager.ScenarioContext);

                try
                {
                    StartFixture(hook, scenarioContainer.Id);
                    var result = base.InvokeBinding(binding, contextManager, arguments, testTracer,
                        out duration);
                    Adapter.StopFixture(x => x.Status = Status.Passed);
                    return result;
                }
                catch (Exception ex)
                {
                    Adapter.StopFixture(x => x.Status = Status.Failed);

                    var scenario = TmsHelper.GetCurrentTestCase(contextManager.ScenarioContext);

                    Adapter.UpdateTestCase(scenario.Id,
                        x =>
                        {
                            x.Status = Status.Failed;
                            x.Message = ex.Message;
                            x.Trace = ex.StackTrace;
                        });
                    throw;
                }
            }

            case HookType.AfterFeature:
                if (hook.HookOrder == int.MaxValue)
                {
                    WriteScenarios(contextManager);

                    return base.InvokeBinding(binding, contextManager, arguments, testTracer, out duration);
                }

                try
                {
                    StartFixture(hook, featureContainerId);
                    var result = base.InvokeBinding(binding, contextManager, arguments, testTracer,
                        out duration);
                    Adapter.StopFixture(x => x.Status = Status.Passed);
                    return result;
                }
                catch (Exception ex)
                {
                    var scenario = contextManager.FeatureContext.Get<HashSet<TestContainer>>().Last();
                    Adapter
                        .StopFixture(x => x.Status = Status.Failed)
                        .UpdateTestCase(scenario.Id,
                            x =>
                            {
                                x.Status = Status.Failed;
                                x.Message = ex.Message;
                                x.Trace = ex.StackTrace;
                            });

                    WriteScenarios(contextManager);

                    throw;
                }

            case HookType.BeforeScenarioBlock:
            case HookType.AfterScenarioBlock:
            case HookType.BeforeTestRun:
            case HookType.AfterTestRun:
            default:
                return base.InvokeBinding(binding, contextManager, arguments, testTracer, out duration);
        }
    }

    private static void StartFixture(HookBinding hook, string containerId)
    {
        if (hook.HookType.ToString().StartsWith("Before"))
            Adapter.StartBeforeFixture(containerId, Hash.NewId(), TmsHelper.GetFixtureResult(hook));
        else
            Adapter.StartAfterFixture(containerId, Hash.NewId(), TmsHelper.GetFixtureResult(hook));
    }

    private static void WriteScenarios(IContextManager contextManager)
    {
        foreach (var c in contextManager.FeatureContext.Get<HashSet<ClassContainer>>())
        {
            var testContainer = contextManager.FeatureContext
                .Get<HashSet<TestContainer>>().FirstOrDefault(t => c.Children.Contains(t.Id));

            Adapter
                .StopTestContainer(c.Id)
                .WriteTestCase(testContainer?.Id!, c.Id);
        }
    }
}