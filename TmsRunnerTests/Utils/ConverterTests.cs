using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using TestIT.ApiClient.Model;
using TmsRunner.Entities.AutoTest;
using TmsRunner.Utils;

namespace TmsRunnerTests.Utils;

[TestClass]
public class ConverterTests
{
    [TestMethod]
    public void ConvertResultToModel()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var result = new AutoTestResult
        {
            Outcome = TestOutcome.Passed
        };

        // Act
        var actual = Converter.ConvertResultToModel(result, id);

        // Assert
        Assert.IsInstanceOfType<AutoTestResultsForTestRunModel>(actual);
        Assert.IsNotNull(actual);
    }

    [TestMethod]
    public void ConvertAutoTestDtoToPutModel()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var autotest = new AutoTest();

        // Act
        var actual = Converter.ConvertAutoTestDtoToPutModel(autotest, id);

        // Assert
        Assert.IsInstanceOfType<UpdateAutoTestRequest>(actual);
        Assert.IsNotNull(actual);
    }

    [TestMethod]
    public void ConvertAutoTestDtoToPostModel()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var autotest = new AutoTest();

        // Act
        var actual = Converter.ConvertAutoTestDtoToPostModel(autotest, id);

        // Assert
        Assert.IsInstanceOfType<CreateAutoTestRequest>(actual);
        Assert.IsNotNull(actual);
    }
}