using Microsoft.Extensions.Logging;
using NSubstitute;
using TestIT.ApiClient.Client;
using Tms.Adapter.Core.Client;
using Tms.Adapter.Core.Configurator;
using Tms.Adapter.Core.Models;
using static System.String;

namespace Tms.Adapter.CoreTests.Client;

[TestClass]
public class ClientTests
{
    private readonly ILogger<TmsClient> _logger = Substitute.For<ILogger<TmsClient>>();
    private readonly TmsSettings _settings = new()
    {
        Url = "https://example.com",
        PrivateToken = "token",
        ProjectId = Guid.NewGuid().ToString(),
        ConfigurationId = Guid.NewGuid().ToString(),
        TestRunId = Guid.NewGuid().ToString()
    };
    
    [TestMethod]
    public async Task IsAutotestExist()
    {
        // Arrange
        var client = new TmsClient(_logger, _settings);
        var id = Guid.NewGuid().ToString();

        // Act & Assert
        await Assert
            .ThrowsExceptionAsync<ApiException>(async () => await client.IsAutotestExist(id).ConfigureAwait(false))
            .ConfigureAwait(false);
    }
    
    [TestMethod]
    public async Task UpdateAutotest()
    {
        // Arrange
        var client = new TmsClient(_logger, _settings);
        var classContainer = new ClassContainer();
        var testContainer = new TestContainer
        {
            ExternalId = Guid.NewGuid().ToString(),
            DisplayName = Empty
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ApiException>(async () =>
            await client.UpdateAutotest(testContainer, classContainer).ConfigureAwait(false)).ConfigureAwait(false);
    }
    
    [TestMethod]
    public async Task CreateAutotest()
    {
        // Arrange
        var client = new TmsClient(_logger, _settings);
        var classContainer = new ClassContainer();
        var testContainer = new TestContainer
        {
            ExternalId = Guid.NewGuid().ToString(),
            DisplayName = Empty
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ApiException>(async () =>
            await client.CreateAutotest(testContainer, classContainer).ConfigureAwait(false)).ConfigureAwait(false);
    }
    
    [TestMethod]
    public async Task UpdateAutotestLinks()
    {
        // Arrange
        var client = new TmsClient(_logger, _settings);
        var id = Guid.NewGuid().ToString();
        var links = new List<Link>();

        // Act & Assert
        await Assert
            .ThrowsExceptionAsync<ApiException>(
                async () => await client.UpdateAutotest(id, links).ConfigureAwait(false)).ConfigureAwait(false);
    }
    
    [TestMethod]
    public async Task TryLinkAutoTestToWorkItems()
    {
        // Arrange
        var client = new TmsClient(_logger, _settings);
        var id = Guid.NewGuid().ToString();
        var workItemIds = Array.Empty<string>();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ApiException>(async () =>
            await client.TryLinkAutoTestToWorkItems(id, workItemIds).ConfigureAwait(false)).ConfigureAwait(false);
    }
    
    [TestMethod]
    public async Task SubmitTestCaseResult()
    {
        // Arrange
        var client = new TmsClient(_logger, _settings);
        var classContainer = new ClassContainer();
        var testContainer = new TestContainer
        {
            ExternalId = Guid.NewGuid().ToString(),
            Status = Status.Passed
        };

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ApiException>(async () =>
                await client.SubmitTestCaseResult(testContainer, classContainer).ConfigureAwait(false))
            .ConfigureAwait(false);
    }
    
    [TestMethod]
    public async Task UploadAttachment()
    {
        // Arrange
        var client = new TmsClient(_logger, _settings);
        var filename = Path.GetRandomFileName();
        var stream = Stream.Null;

        // Act & Assert
        await Assert
            .ThrowsExceptionAsync<ApiException>(async () =>
                await client.UploadAttachment(filename, stream).ConfigureAwait(false)).ConfigureAwait(false);
    }
    
    [TestMethod]
    public async Task CreateTestRun()
    {
        // Arrange
        var client = new TmsClient(_logger, _settings);

        // Act & Assert
        await client.CreateTestRun().ConfigureAwait(false);
    }
    
    [TestMethod]
    public async Task CompleteTestRun()
    {
        // Arrange
        var client = new TmsClient(_logger, _settings);

        // Act & Assert
        await client.CompleteTestRun().ConfigureAwait(false);
    }
}