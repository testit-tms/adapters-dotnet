using TestIT.ApiClient.Model;

namespace TmsRunner.Services;

public interface ITestRunContextService
{
    void SetCurrentTestRun(TestRunV2ApiResult testRun);
    TestRunV2ApiResult? GetCurrentTestRun();
} 