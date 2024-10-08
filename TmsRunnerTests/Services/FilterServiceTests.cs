using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NSubstitute;
using Tms.Adapter.Utils;
using TmsRunner.Entities.Configuration;
using TmsRunner.Services;

namespace TmsRunnerTests.Services;

[TestClass]
public class FilterServiceTests
{
    private readonly ILogger<FilterService> _logger = Substitute.For<ILogger<FilterService>>();
    private readonly Replacer _replacer = Substitute.For<Replacer>();

    [TestMethod]
    public void FilterTestCases()
    {
        // Arrange
        var filterService = new FilterService(_logger, _replacer);
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
        var filterService = new FilterService(_logger, _replacer);
        var assemblyPath = typeof(FilterServiceTests).Assembly.Location;
        var config = new AdapterConfig
        {
            TestAssemblyPath = typeof(FilterServiceTests).Assembly.Location
        };
        var testcases = new[] { new TestCase() };

        // Act
        var actual = filterService.FilterTestCasesByLabels(config, testcases);

        // Assert
        Assert.AreEqual(0, actual.Count);
    }
}