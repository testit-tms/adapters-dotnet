using Tms.Adapter.Core.Service;

namespace Tms.Adapter.CoreTests.Service;

[TestClass]
public class AdapterTests
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
    }

    [TestMethod]
    public void AddMessage()
    {
        const string message = "test";

        Core.Service.Adapter.AddMessage(message);
    }

    [TestMethod]
    public void AddAttachments()
    {
        var filename = Path.GetRandomFileName();

        try
        {
            File.Create(filename).Dispose();

            // No step context → storage lookup fails; do not spin up SyncStorage in unit tests.
            Assert.ThrowsException<ArgumentNullException>(() => Core.Service.Adapter.AddAttachments(filename));
        }
        finally
        {
            File.Delete(filename);
        }
    }

    [TestMethod]
    public void AddLinks()
    {
        const string url = "https://example.com";

        Core.Service.Adapter.AddLinks(url);
    }
}
