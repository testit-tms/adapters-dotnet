using Moq;
using Tms.Adapter.XUnit;
using Xunit.Abstractions;
using ITestMethod = Xunit.Abstractions.ITestMethod;

namespace Tms.Adapter.XUnitTests;

[TestClass]
public class XunitDiscoverTests
{
    [TestMethod]
    public void XunitDiscover()
    {
        // Arrange
        var messageSinkMock = new Mock<IMessageSink>();
        var discover = new XunitDiscover(messageSinkMock.Object);

        // Act
        var testCases = discover.Discover(Mock.Of<ITestFrameworkDiscoveryOptions>(),
            Mock.Of<ITestMethod>(),
            Mock.Of<IAttributeInfo>());

        // Assert
        Assert.IsInstanceOfType<TmsXunitTestCase>(testCases.First());
        Assert.IsNotNull(testCases.First());
    }

    [TestMethod]
    public void XunitTheoryDiscover()
    {
        // Arrange
        var messageSinkMock = new Mock<IMessageSink>();
        var discover = new XunitTheoryDiscover(messageSinkMock.Object);

        // Act
        var testCases = discover.Discover(
            Mock.Of<ITestFrameworkDiscoveryOptions>(
                o => o.GetValue<bool?>(It.IsAny<string>()) == false),
            Mock.Of<ITestMethod>(m => m.Method.GetCustomAttributes(
                It.IsAny<string>()) == new List<IAttributeInfo>() { }),
            Mock.Of<IAttributeInfo>()
        );

        // Assert
        Assert.IsInstanceOfType<TmsXunitTestCase>(testCases.First());
        Assert.IsNotNull(testCases.First());
    }
}
