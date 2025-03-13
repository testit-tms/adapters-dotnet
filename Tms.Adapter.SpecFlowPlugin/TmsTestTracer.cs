using System.Globalization;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.BindingSkeletons;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.Tracing;
using Tms.Adapter.Core.Models;
using Tms.Adapter.Core.Service;
using Tms.Adapter.Core.Utils;

namespace Tms.Adapter.SpecFlowPlugin;

public class TmsTestTracer : TestTracer, ITestTracer
{
    private static readonly AdapterManager Adapter = AdapterManager.Instance;
    private const string NoMatchingStepMessage = "No matching step definition found for the step";

    public TmsTestTracer(ITraceListener traceListener, IStepFormatter stepFormatter,
        IStepDefinitionSkeletonProvider stepDefinitionSkeletonProvider, SpecFlowConfiguration specFlowConfiguration)
        : base(traceListener, stepFormatter, stepDefinitionSkeletonProvider, specFlowConfiguration)
    {
    }

    void ITestTracer.TraceStep(StepInstance stepInstance, bool showAdditionalArguments)
    {
        TraceStep(stepInstance, showAdditionalArguments);

        var stepResult = new StepResult
        {
            DisplayName = $"{stepInstance.Keyword} {stepInstance.Text}"
        };

        Adapter.StartStep(Hash.NewId(), stepResult);
    }

    void ITestTracer.TraceStepDone(BindingMatch match, object[] arguments, TimeSpan duration)
    {
        TraceStepDone(match, arguments, duration);

        Adapter.StopStep(x => x.Status = Status.Passed);
    }

    void ITestTracer.TraceError(Exception ex, TimeSpan duration)
    {
        TraceError(ex, duration);

        Adapter.StopStep(x => x.Status = Status.Failed);
        Adapter.UpdateTestCase(
            x =>
            {
                x.Status = Status.Failed;
                x.Message = ex.Message;
                x.Trace = ex.StackTrace;
            });
    }

    void ITestTracer.TraceStepSkipped()
    {
        TraceStepSkipped();

        Adapter.StopStep(x => x.Status = Status.Skipped);
    }

    void ITestTracer.TraceStepPending(BindingMatch match, object[] arguments)
    {
        TraceStepPending(match, arguments);

        Adapter.StopStep(x => x.Status = Status.Skipped);
    }

    void ITestTracer.TraceNoMatchingStepDefinition(StepInstance stepInstance, ProgrammingLanguage targetLanguage,
        CultureInfo bindingCulture, List<BindingMatch> matchesWithoutScopeCheck)
    {
        TraceNoMatchingStepDefinition(stepInstance, targetLanguage, bindingCulture, matchesWithoutScopeCheck);

        Adapter.StopStep(x => x.Status = Status.Failed);
        Adapter.UpdateTestCase(x =>
        {
            x.Status = Status.Failed;
            x.Message = NoMatchingStepMessage;
        });
    }
}