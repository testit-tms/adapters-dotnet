using Microsoft.Extensions.Logging;
using NSubstitute;
using Tms.Adapter.Core.Client;
using Tms.Adapter.Core.Models;

namespace Tms.Adapter.CoreTests.Writer;

[TestClass]
public class WriterTests
{
    private readonly ILogger<Core.Writer.Writer> _logger = Substitute.For<ILogger<Core.Writer.Writer>>();
    private readonly ITmsClient _client = Substitute.For<ITmsClient>();
    private readonly ClassContainer _classContainer = Substitute.For<ClassContainer>();
    private readonly TestContainer _testContainer = Substitute.For<TestContainer>();

    [TestMethod]
    public async Task WriteResults()
    {
        // Arrange
        var writer = new Core.Writer.Writer(_logger, _client);
        
        // Act & Assert
        await writer.Write(_testContainer, _classContainer).ConfigureAwait(false);
    }
}