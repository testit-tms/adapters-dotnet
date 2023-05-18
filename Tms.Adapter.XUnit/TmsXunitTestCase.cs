using System.ComponentModel;
using Tms.Adapter.Core.Models;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Tms.Adapter.XUnit;

public class TmsXunitTestCase : XunitTestCase, ITmsAccessor
{
    public ClassContainer ClassContainer { get; set; }

    public TestContainer TestResult { get; set; }

#pragma warning disable CS0618
    [EditorBrowsable(EditorBrowsableState.Never)]
    public TmsXunitTestCase()
#pragma warning restore
    {
    }
    
    public TmsXunitTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay testMethodDisplay,
        TestMethodDisplayOptions defaultMethodDisplayOptions,
        ITestMethod testMethod, object[] testMethodArguments = null)
        : base(diagnosticMessageSink, testMethodDisplay, defaultMethodDisplayOptions, testMethod,
            testMethodArguments)
    {
    }

    public override async Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink,
        IMessageBus messageBus,
        object[] constructorArguments,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource)
    {
        Steps.TestResultAccessor = this;
        messageBus = new TmsMessageBus(messageBus);
        var summary = await base.RunAsync(diagnosticMessageSink, messageBus, constructorArguments, aggregator,
            cancellationTokenSource);
        return summary;
    }
}