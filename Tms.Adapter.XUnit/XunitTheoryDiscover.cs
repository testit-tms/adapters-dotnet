using Xunit.Abstractions;
using Xunit.Sdk;

namespace Tms.Adapter.XUnit;

public class XunitTheoryDiscover : TheoryDiscoverer
{
    public XunitTheoryDiscover(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink)
    {
    }

    public override IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions,
        ITestMethod testMethod, IAttributeInfo factAttribute)
    {
        var testCases = base.Discover(discoveryOptions, testMethod, factAttribute);

        foreach (var testCase in testCases)
        {
            var dataAttribute = testCase.TestMethod.Method
                .GetCustomAttributes(typeof(DataAttribute)).FirstOrDefault() as IReflectionAttributeInfo;

            if (dataAttribute?.Attribute is DataAttribute memberDataAttribute && testCase.TestMethodArguments is null)
            {
                var argumentSets = memberDataAttribute
                    .GetData(testCase.TestMethod.Method.ToRuntimeMethod());

                foreach (var arguments in argumentSets)
                {
                    var tmsTestCase = new TmsXunitTestCase(DiagnosticMessageSink,
                        discoveryOptions.MethodDisplayOrDefault(),
                        TestMethodDisplayOptions.None, testMethod, arguments);
                    yield return tmsTestCase;
                }
            }
            else
            {
                var tmsTestCase = new TmsXunitTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(),
                    TestMethodDisplayOptions.None, testMethod, testCase.TestMethodArguments);
                yield return tmsTestCase;
            }
        }
    }
}