using Tms.Adapter.Core.Models;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Tms.Adapter.XUnit;

public class TmsXunitTestCase : XunitTestCase, ITmsAccessor
{
    public ClassContainer ClassContainer { get; set; }
    public TestContainer TestResult { get; set; }

    public TmsXunitTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay testMethodDisplay,
        TestMethodDisplayOptions defaultMethodDisplayOptions,
        ITestMethod testMethod, object[] testMethodArguments = null)
        : base(diagnosticMessageSink, testMethodDisplay, defaultMethodDisplayOptions, testMethod,
            testMethodArguments)
    {
    }

    public TmsXunitTestCase() { }

    public override async Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink,
        IMessageBus messageBus,
        object[] constructorArguments,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource)
    {
        StepManager.TestResultAccessor = this;
        messageBus = new TmsMessageBus(messageBus);
        var summary = await base.RunAsync(diagnosticMessageSink, messageBus, constructorArguments, aggregator,
            cancellationTokenSource);
        return summary;
    }
}