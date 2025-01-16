using Microsoft.Extensions.Logging;
using Moq;
using Tms.Adapter.Core.Client;
using Tms.Adapter.Core.Configurator;
using Tms.Adapter.Core.Models;

namespace Tms.Adapter.CoreTests.Writer;

[TestClass]
public class WriterTests
{
    private readonly Mock<ClassContainer> _classContainer = new();
    private readonly Mock<ITmsClient> _client = new();
    private readonly Mock<ILogger<Core.Writer.Writer>> _logger = new();
    private readonly Mock<TmsSettings> _settings = new();
    private readonly Mock<TestContainer> _testContainer = new();

    [TestMethod]
    public async Task WriteResults()
    {
        // Arrange
        var writer = new Core.Writer.Writer(_logger.Object, _client.Object, _settings.Object);

        // Act & Assert
        await writer.Write(_testContainer.Object, _classContainer.Object).ConfigureAwait(false);
    }
}