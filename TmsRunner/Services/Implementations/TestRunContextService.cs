using TestIT.ApiClient.Model;

namespace TmsRunner.Services.Implementations;

internal class TestRunContextService : ITestRunContextService
{
    private TestRunV2GetModel? _currentTestRun;

    public void SetCurrentTestRun(TestRunV2GetModel testRun)
    {
        _currentTestRun = testRun;
    }

    public TestRunV2GetModel? GetCurrentTestRun()
    {
        return _currentTestRun;
    }
} 