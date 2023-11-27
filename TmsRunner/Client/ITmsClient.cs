using TestIT.ApiClient.Model;
using TmsRunner.Models;

namespace TmsRunner.Client;

public interface ITmsClient
{
    Task<string> CreateTestRun();
    Task<List<string>> GetAutoTestsForRun(string testRunId);
    Task SubmitResultToTestRun(string guid, AutoTestResult result);
    Task<AttachmentModel> UploadAttachment(string fileName, Stream content);
    Task<AutoTestModel?> GetAutotestByExternalId(string externalId);
    Task<AutoTestModel> CreateAutotest(AutoTest model);
    Task UpdateAutotest(AutoTest model);
    Task LinkAutoTestToWorkItem(string autotestId, string workItemId);
}