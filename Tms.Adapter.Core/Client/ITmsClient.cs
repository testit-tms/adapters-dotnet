using Tms.Adapter.Core.Models;

namespace Tms.Adapter.Core.Client;

public interface ITmsClient
{
    Task<bool> IsAutotestExist(string externalId);
    Task CreateAutotest(TestContainer result, ClassContainer container);
    Task UpdateAutotest(TestContainer result, ClassContainer container);
    Task UpdateAutotest(string externalId, List<Link> links);
    Task LinkAutoTestToWorkItems(string externalId, IEnumerable<string> workItemIds);
    Task SubmitTestCaseResult(TestContainer result, ClassContainer container);
    Task<string> UploadAttachment(string fileName, Stream content);
}