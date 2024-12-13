using TestIT.ApiClient.Model;
using Tms.Adapter.Core.Models;
using Link = Tms.Adapter.Core.Models.Link;

namespace Tms.Adapter.Core.Client;

public interface ITmsClient
{
    Task<bool> IsAutotestExist(string externalId);
    Task CreateAutotest(TestContainer result, ClassContainer container);
    Task UpdateAutotest(TestContainer result, ClassContainer container);
    Task UpdateAutotest(string externalId, List<Link> links, string externalKey);
    Task LinkAutoTestToWorkItems(string autotestId, IEnumerable<string> workItemIds);
    Task DeleteAutoTestLinkFromWorkItem(string autotestId, string workItemId);
    Task<List<WorkItemIdentifierModel>> GetWorkItemsLinkedToAutoTest(string autotestId);
    Task SubmitTestCaseResult(TestContainer result, ClassContainer container);
    Task<string> UploadAttachment(string fileName, Stream content);
    Task CreateTestRun();
    Task CompleteTestRun();
    Task<AutoTestModel?> GetAutotestByExternalId(string externalId);
}