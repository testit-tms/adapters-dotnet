using Tms.Adapter.Core.Configurator;

namespace Tms.Adapter.CoreTests.Configurator;

[TestClass]
public class ConfiguratorTests
{
    [TestInitialize]
    public void InitializeTest()
    {
        Environment.SetEnvironmentVariable("TMS_URL", "https://example.com");
        Environment.SetEnvironmentVariable("TMS_PRIVATE_TOKEN", "token");
        Environment.SetEnvironmentVariable("TMS_PROJECT_ID", Guid.NewGuid().ToString());
        Environment.SetEnvironmentVariable("TMS_CONFIGURATION_ID", Guid.NewGuid().ToString());
        Environment.SetEnvironmentVariable("TMS_TEST_RUN_ID", Guid.NewGuid().ToString());
    }

    [TestMethod]
    public void GetConfig()
    {
        // Act
        var actual = Core.Configurator.Configurator.GetConfig();

        // Assert
        Assert.IsInstanceOfType<TmsSettings>(actual);
        Assert.IsNotNull(actual);
    }

    [TestMethod]
    public void GetConfig_ImportRealtimeDefault_IsTrue()
    {
        Environment.SetEnvironmentVariable("TMS_IMPORT_REALTIME", null);

        var actual = Core.Configurator.Configurator.GetConfig();

        Assert.IsTrue(actual.ImportRealtime);
    }

    [TestMethod]
    public void GetConfig_ImportRealtimeEnvFalse_IsFalse()
    {
        Environment.SetEnvironmentVariable("TMS_IMPORT_REALTIME", "false");

        var actual = Core.Configurator.Configurator.GetConfig();

        Assert.IsFalse(actual.ImportRealtime);
    }
}