using TestIT.ApiClient.Model;

namespace TmsRunner.Services.Interfaces;

public interface ITestRunContextService
{
    void SetCurrentTestRun(TestRunV2GetModel testRun);
    TestRunV2GetModel? GetCurrentTestRun();
} 