using Tms.Adapter.Core.Models;

namespace Tms.Adapter.Core.Client;

public interface ITmsClient
{
    TestResult? GetAutotestExist(string externalId);
    void CreateAutotest(TestResult result);
    void UpdateAutotest(TestResult result);
    void LinkAutoTestToWorkItem(string externalId, string workItemId);
}