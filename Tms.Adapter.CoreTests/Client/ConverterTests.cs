using TestIT.ApiClient.Model;
using Tms.Adapter.Core.Client;
using Tms.Adapter.Core.Models;
using static System.String;

namespace Tms.Adapter.CoreTests.Client;

[TestClass]
public class ConverterTests
{
    [TestMethod]
    public void ConvertAutoTestDtoToPostModel()
    {
        // Arrange
        var classContainer = new ClassContainer();
        var testContainer = new TestContainer
        {
            Id = "",
            ExternalId = Guid.NewGuid().ToString(),
            DisplayName = Empty
        };
        var id = Guid.NewGuid().ToString();

        // Act
        var actual = Converter.ConvertAutoTestDtoToPostModel(testContainer, classContainer, id);

        // Assert
        Assert.IsInstanceOfType<AutoTestPostModel>(actual);
        Assert.IsNotNull(actual);
    }

    [TestMethod]
    public void ConvertResultToModel()
    {
        // Arrange
        var classContainer = new ClassContainer();
        var testContainer = new TestContainer
        {
            Id = "",
            ExternalId = Guid.NewGuid().ToString(),
            Status = Status.Passed
        };
        var id = Guid.NewGuid().ToString();

        // Act
        var actual = Converter.ConvertResultToModel(testContainer, classContainer, id);

        // Assert
        Assert.IsInstanceOfType<AutoTestResultsForTestRunModel>(actual);
        Assert.IsNotNull(actual);
    }

    [TestMethod]
    public void ConvertAutoTestDtoToPutModel()
    {
        // Arrange
        var classContainer = new ClassContainer();
        var testContainer = new TestContainer
        {
            Id = "",
            ExternalId = Guid.NewGuid().ToString(),
            DisplayName = Empty
        };
        var id = Guid.NewGuid().ToString();

        // Act
        var actual = Converter.ConvertAutoTestDtoToPutModel(testContainer, classContainer, id);

        // Assert
        Assert.IsInstanceOfType<AutoTestPutModel>(actual);
        Assert.IsNotNull(actual);
    }
}