using TestIT.AdaptersApi.Model;

namespace TmsRunner.Services.Implementations;

internal sealed class TestRunContextService : ITestRunContextService
{
    private TestRunApiResult? _currentTestRun;
    private IReadOnlyList<TestResultResponse> _testResults = [];

    public void SetCurrentTestRun(TestRunApiResult testRun)
    {
        _currentTestRun = testRun;
    }

    public TestRunApiResult? GetCurrentTestRun() => _currentTestRun;

    public void SetTestResults(IReadOnlyList<TestResultResponse> testResults)
    {
        _testResults = testResults;
    }

    public IReadOnlyList<TestResultResponse> GetTestResults() => _testResults;
}
