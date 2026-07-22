using TestIT.AdaptersApi.Model;

namespace TmsRunner.Services;

public interface ITestRunContextService
{
    void SetCurrentTestRun(TestRunApiResult testRun);
    TestRunApiResult? GetCurrentTestRun();
    void SetTestResults(IReadOnlyList<TestResultResponse> testResults);
    IReadOnlyList<TestResultResponse> GetTestResults();
}
