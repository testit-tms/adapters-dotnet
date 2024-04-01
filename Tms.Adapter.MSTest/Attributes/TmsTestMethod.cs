using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tms.Adapter.MSTest.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class TmsTestMethod : TestMethodAttribute
{
    public override TestResult[] Execute(ITestMethod testMethod)
    {
        var containerId = TmsHelper.StartTestContainer();
        var testResultId = TmsHelper.StartTestCase(containerId, testMethod);

        var testResult = new TestResult[1] { testMethod.Invoke(null) };

        TmsHelper.UpdateTestCase(testResultId, testResult[0]);
        TmsHelper.FinishTestCase(testResultId, containerId);

        return testResult;
    }
}
