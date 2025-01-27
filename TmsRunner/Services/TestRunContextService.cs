using TestIT.ApiClient.Model;

namespace TmsRunner.Services;

public interface ITestRunContextService
{
    void SetCurrentTestRun(TestRunV2GetModel testRun);
    TestRunV2GetModel? GetCurrentTestRun();
}

public class TestRunContextService : ITestRunContextService
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