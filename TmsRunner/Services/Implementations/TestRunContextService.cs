using TestIT.ApiClient.Model;

namespace TmsRunner.Services.Implementations;

internal class TestRunContextService : ITestRunContextService
{
    private TestRunV2ApiResult? _currentTestRun;

    public void SetCurrentTestRun(TestRunV2ApiResult testRun)
    {
        _currentTestRun = testRun;
    }

    public TestRunV2ApiResult? GetCurrentTestRun()
    {
        return _currentTestRun;
    }
} 