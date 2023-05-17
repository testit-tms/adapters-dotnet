using Tms.Adapter.Core.Models;

namespace Tms.Adapter.Core.Client;

public interface ITmsClient
{
    Task CreatTestRun();
    Task<bool> IsAutotestExist(string externalId);
    Task CreateAutotest(TestResult result, TestResultContainer container);
    Task UpdateAutotest(TestResult result, TestResultContainer container);
    Task UpdateAutotest(string externalId, List<Link> links);
    Task LinkAutoTestToWorkItems(string externalId, IEnumerable<string> workItemIds);
    Task SubmitTestCaseResult(TestResult result, TestResultContainer container);
}