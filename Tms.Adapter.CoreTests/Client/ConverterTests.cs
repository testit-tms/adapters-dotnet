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
            ExternalId = Guid.NewGuid().ToString(),
            DisplayName = Empty
        };
        var id = Guid.NewGuid().ToString();

        // Act
        var actual = Converter.ConvertAutoTestDtoToPostModel(testContainer, classContainer, id);

        // Assert
        Assert.IsInstanceOfType<AutoTestCreateApiModel>(actual);
        Assert.IsNotNull(actual);
    }

    [TestMethod]
    public void ConvertResultToModel()
    {
        // Arrange
        var classContainer = new ClassContainer();
        var testContainer = new TestContainer
        {
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
            ExternalId = Guid.NewGuid().ToString(),
            DisplayName = Empty
        };
        var id = Guid.NewGuid().ToString();

        // Act
        var actual = Converter.ConvertAutoTestDtoToPutModel(testContainer, classContainer, id);

        // Assert
        Assert.IsInstanceOfType<AutoTestUpdateApiModel>(actual);
        Assert.IsNotNull(actual);
    }

    [TestMethod]
    public void ToTestResultCutApiModel_FromContainer_MapsUndefinedToPassedAndSucceeded()
    {
        var container = new TestContainer
        {
            ExternalId = "ext-undefined",
            Status = Status.Undefined,
            Start = 1_700_000_000_000
        };
        var projectId = Guid.NewGuid().ToString();

        var actual = Converter.ToTestResultCutApiModel(container, projectId);

        Assert.AreEqual(projectId, actual.ProjectId);
        Assert.AreEqual("ext-undefined", actual.AutoTestExternalId);
        Assert.AreEqual("Passed", actual.StatusCode);
        Assert.AreEqual("Succeeded", actual.StatusType);
        Assert.IsNotNull(actual.StartedOn);
    }

    [TestMethod]
    public void ToTestResultCutApiModel_FromArgs_MapsSkippedToIncomplete()
    {
        var projectId = Guid.NewGuid().ToString();

        var actual = Converter.ToTestResultCutApiModel("ext-skipped", "Skipped", DateTime.UtcNow, projectId);

        Assert.AreEqual(projectId, actual.ProjectId);
        Assert.AreEqual("Skipped", actual.StatusCode);
        Assert.AreEqual("Incomplete", actual.StatusType);
    }

    [TestMethod]
    public void ToTestResultCutApiModel_FromContainer_ThrowsWhenProjectIdMissing()
    {
        var container = new TestContainer { ExternalId = "ext-1", Status = Status.Passed };

        Assert.ThrowsException<ArgumentException>(() => Converter.ToTestResultCutApiModel(container, ""));
    }

    [TestMethod]
    public void ToTestResultCutApiModel_FromArgs_ThrowsWhenProjectIdMissing()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            Converter.ToTestResultCutApiModel("ext-1", "Passed", DateTime.UtcNow, " "));
    }
}