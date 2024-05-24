﻿using Tms.Adapter;

namespace TmsRunnerTests;

[TestClass]
public class AdapterTests
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
    public void AddMessage()
    {
        // Arrange
        const string message = "test";

        // Act & Assert
        Adapter.AddMessage(message);
    }

    [TestMethod]
    public void AddAttachments()
    {
        var filename = Path.GetRandomFileName();

        try
        {
            // Arrange
            File.Create(filename).Dispose();

            // Act & Assert
            Adapter.AddAttachments(filename);
        }
        finally
        {
            File.Delete(filename);
        }
    }

    [TestMethod]
    public void AddLinks()
    {
        // Arrange
        const string url = "https://example.com";

        // Act & Assert
        Adapter.AddLinks(url);
    }
}