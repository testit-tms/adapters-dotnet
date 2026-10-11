using TestIT.AdaptersApi.Model;

namespace TmsRunner.Services.Implementations;

internal sealed class TestRunContextService : ITestRunContextService
{
    private TestRunApiResult? _currentTestRun;
    private IReadOnlyList<TestResultShortResponse> _testResults = [];

    public void SetCurrentTestRun(TestRunApiResult testRun)
    {
        _currentTestRun = testRun;
    }

    public TestRunApiResult? GetCurrentTestRun() => _currentTestRun;

    public void SetTestResults(IReadOnlyList<TestResultShortResponse> testResults)
    {
        _testResults = testResults;
    }

    public IReadOnlyList<TestResultShortResponse> GetTestResults() => _testResults;
}
