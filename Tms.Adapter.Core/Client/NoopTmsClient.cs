using System.Collections.Generic;
using TestIT.ApiClient.Model;
using Tms.Adapter.Core.Models;
using Link = Tms.Adapter.Core.Models.Link;

namespace Tms.Adapter.Core.Client;

internal sealed class NoopTmsClient : ITmsClient
{
    public Task<bool> IsAutotestExist(string externalId) => Task.FromResult(false);
    public Task CreateAutotest(TestContainer result, ClassContainer container) => Task.CompletedTask;
    public Task UpdateAutotest(TestContainer result, ClassContainer container) => Task.CompletedTask;
    public Task UpdateAutotest(string externalId, List<Link> links, string externalKey) => Task.CompletedTask;
    public Task LinkAutoTestToWorkItems(string autotestId, IEnumerable<string> workItemIds) => Task.CompletedTask;
    public Task DeleteAutoTestLinkFromWorkItem(string autotestId, string workItemId) => Task.CompletedTask;
    public Task<List<AutoTestWorkItemIdentifierApiResult>> GetWorkItemsLinkedToAutoTest(string autotestId) =>
        Task.FromResult(new List<AutoTestWorkItemIdentifierApiResult>());
    public Task SubmitTestCaseResult(TestContainer result, ClassContainer container) => Task.CompletedTask;
    public Task<string> UploadAttachment(string fileName, Stream content) => Task.FromResult(string.Empty);
    public Task CreateTestRun() => Task.CompletedTask;
    public Task UpdateTestRun() => Task.CompletedTask;
    public Task CompleteTestRun() => Task.CompletedTask;
    public Task<AutoTestApiResult?> GetAutotestByExternalId(string externalId) => Task.FromResult<AutoTestApiResult?>(null);
}

