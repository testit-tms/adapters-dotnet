using Microsoft.Extensions.Logging;
using LoggerFactory = Tms.Adapter.Core.Logger.LoggerFactory;

namespace Tms.Adapter.CoreTests.Logger;

[TestClass]
public class LoggerTests
{
    [TestMethod]
    public void GetLogger()
    {
        // Act
        var actual = LoggerFactory.GetLogger();

        // Assert
        Assert.IsInstanceOfType<ILoggerFactory>(actual);
        Assert.IsNotNull(actual);
    }
}