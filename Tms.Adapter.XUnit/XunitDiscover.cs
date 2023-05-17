using Xunit.Abstractions;
using Xunit.Sdk;

namespace Tms.Adapter.XUnit;

public class XunitDiscover : IXunitTestCaseDiscoverer
{
    private readonly IMessageSink _diagnosticMessageSink;

    public XunitDiscover(IMessageSink diagnosticMessageSink)
    {
        _diagnosticMessageSink = diagnosticMessageSink;
    }

    public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod,
        IAttributeInfo factAttribute)
    {
        var testCase = new TmsXunitTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(),
            TestMethodDisplayOptions.None, testMethod);

        return new[] { testCase };
    }
}