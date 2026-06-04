using Tms.Adapter.Core.Models;
using Tms.Adapter.Core.Service;
using Tms.Adapter.Core.Utils;

namespace Tms.Adapter.CoreTests.Service;

[TestClass]
public class AdapterManagerImportRealtimeTests
{
    [TestInitialize]
    public void InitializeTest()
    {
        AdapterManager.ClearInstance();
        Environment.SetEnvironmentVariable("TMS_DISABLE_NETWORK", "true");
        Environment.SetEnvironmentVariable("TMS_URL", "https://example.com");
        Environment.SetEnvironmentVariable("TMS_PRIVATE_TOKEN", "token");
        Environment.SetEnvironmentVariable("TMS_PROJECT_ID", Guid.NewGuid().ToString());
        Environment.SetEnvironmentVariable("TMS_CONFIGURATION_ID", Guid.NewGuid().ToString());
        Environment.SetEnvironmentVariable("TMS_TEST_RUN_ID", Guid.NewGuid().ToString());
    }

    [TestCleanup]
    public void CleanupTest()
    {
        AdapterManager.ClearInstance();
        Environment.SetEnvironmentVariable("TMS_DISABLE_NETWORK", null);
        Environment.SetEnvironmentVariable("TMS_IMPORT_REALTIME", null);
    }

    [TestMethod]
    public void FlushBufferedTestCases_WhenImportRealtimeFalse_DoesNotThrow()
    {
        Environment.SetEnvironmentVariable("TMS_IMPORT_REALTIME", "false");
        var adapter = AdapterManager.Instance;

        var classContainer = new ClassContainer { Id = Hash.NewId() };
        var testContainer = new TestContainer
        {
            Id = Hash.NewId(),
            ExternalId = "bulk-test-1",
            Status = Status.Passed
        };

        adapter.StartTestContainer(classContainer);
        adapter.StartTestCase(testContainer);
        adapter.StopTestCase(testContainer.Id);
        adapter.WriteTestCase(testContainer.Id, classContainer.Id);

        adapter.FlushBufferedTestCases();
        adapter.FlushBufferedTestCases();
    }

    [TestMethod]
    public void FlushBufferedTestCases_WhenImportRealtimeTrue_IsNoOp()
    {
        Environment.SetEnvironmentVariable("TMS_IMPORT_REALTIME", "true");
        var adapter = AdapterManager.Instance;

        adapter.FlushBufferedTestCases();
    }
}
