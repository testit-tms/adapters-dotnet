using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Moq;
using Tms.Adapter.Utils;
using TmsRunner.Entities.Configuration;
using TmsRunner.Services;

namespace TmsRunnerTests.Services;

[TestClass]
public class FilterServiceTests
{
    private readonly Mock<ILogger<FilterService>> _logger = new();
    private readonly Mock<Replacer> _replacer = new();

    [TestMethod]
    public void FilterTestCases()
    {
        // Arrange
        var filterService = new FilterService(_logger.Object, _replacer.Object);
        var assemblyPath = typeof(FilterServiceTests).Assembly.Location;
        var externalId = new[] { "123" };
        var testcases = new[] { new TestCase { FullyQualifiedName = "test" } };

        // Act
        var actual = filterService.FilterTestCases(assemblyPath, externalId, testcases);

        // Assert
        Assert.AreEqual(0, actual.Count);
    }

    [TestMethod]
    public void FilterTestCasesByLabels()
    {
        // Arrange
        var filterService = new FilterService(_logger.Object, _replacer.Object);
        var config = new AdapterConfig
        {
            TestAssemblyPath = typeof(FilterServiceTests).Assembly.Location
        };
        var testcases = new[] { new TestCase { FullyQualifiedName = "test" } };

        // Act
        var actual = filterService.FilterTestCasesByLabels(config, testcases);

        // Assert
        Assert.AreEqual(0, actual.Count);
    }
}