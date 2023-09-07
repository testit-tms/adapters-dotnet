using TestIt.Client.Model;
using TmsRunner.Models;

namespace TmsRunner.Client;

public interface ITmsClient
{
    Task<TestRunV2GetModel> CreateTestRun();
    Task<List<string>> GetAutoTestsForRun(string testRunId);
    Task SubmitResultToTestRun(TestRunV2GetModel testRun, AutoTestResult result);
    Task<AttachmentModel> UploadAttachment(string fileName, Stream content);
    Task<AutoTestModel?> GetAutotestByExternalId(string externalId);
    Task<AutoTestModel> CreateAutotest(AutoTest model);
    Task UpdateAutotest(AutoTest model);
    Task LinkAutoTestToWorkItem(string autotestId, string workItemId);
    Task<ProjectModel> GetProject();
    Task<TestRunV2GetModel> GetTestRun(string id);
}