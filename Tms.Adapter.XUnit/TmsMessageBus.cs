using Xunit.Abstractions;
using Xunit.Sdk;

namespace Tms.Adapter.XUnit;

public class TmsMessageBus : IMessageBus
{
    private readonly IMessageBus _inner;

    public TmsMessageBus(IMessageBus inner)
    {
        _inner = inner;
    }

    public void Dispose()
    {
        _inner.Dispose();
        GC.SuppressFinalize(this);
    }
    public bool QueueMessage(IMessageSinkMessage message)
    {
        switch (message)
        {
            case ITestCaseStarting testCaseStarting:
                TmsXunitHelper.StartTestContainer(testCaseStarting);
                break;
            case ITestClassConstructionFinished testClassConstructionFinished:
                TmsXunitHelper.StartTestCase(testClassConstructionFinished);
                break;
            case ITestFailed testFailed:
                TmsXunitHelper.MarkTestCaseAsFailed(testFailed);
                break;
            case ITestPassed testPassed:
                TmsXunitHelper.MarkTestCaseAsPassed(testPassed);
                break;
            case ITestCaseFinished testCaseFinished:
                if (testCaseFinished.TestCase.SkipReason != null)
                {
                    TmsXunitHelper.StartTestCase(testCaseFinished);
                    TmsXunitHelper.MarkTestCaseAsSkipped(testCaseFinished);
                }
                TmsXunitHelper.FinishTestCase(testCaseFinished);
                break;
        }

        return _inner.QueueMessage(message);
    }
}