using Tms.Adapter.XUnit;
using Tms.Adapter.Core.Models;
using Xunit.Sdk;
using Tms.Adapter.Core.Service;
using Moq;
using Tms.Adapter.Core.Utils;

namespace Tms.Adapter.XUnitTests;

[TestClass]
public class StepManagerTests
{
    private readonly ITmsAccessor _testCase = Mock.Of<ITmsAccessor>();

    public StepManagerTests()
    {
        _testCase.ClassContainer = new ClassContainer
        {
            Id = Guid.NewGuid().ToString(),
        };

        _testCase.TestResult = new TestContainer
        {
            Id = Guid.NewGuid().ToString(),
        };

        StepManager.TestResultAccessor = _testCase;
    }

    [TestInitialize]
    public void TestSetup()
    {
        Environment.SetEnvironmentVariable("TMS_URL", "https://example.com");
        Environment.SetEnvironmentVariable("TMS_PRIVATE_TOKEN", "token");
        Environment.SetEnvironmentVariable("TMS_PROJECT_ID", Guid.NewGuid().ToString());
        Environment.SetEnvironmentVariable("TMS_CONFIGURATION_ID", Guid.NewGuid().ToString());
        Environment.SetEnvironmentVariable("TMS_TEST_RUN_ID", Guid.NewGuid().ToString());

        AdapterManager.Instance.StartTestContainer(_testCase.ClassContainer);
        AdapterManager.Instance.StartTestCase(_testCase.ClassContainer.Id, _testCase.TestResult);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        AdapterManager.ClearInstance();
    }

    [TestMethod]
    public void StartBeforeFixture()
    {
        // Arrange
        var name = RandomUtils.GetRandomString();

        // Act
        StepManager.StartBeforeFixture(name);

        // Assert
        Assert.AreEqual(_testCase.ClassContainer.Befores.First().DisplayName, name);
        Assert.AreNotEqual(_testCase.ClassContainer.Befores.First().Start, 0);
        Assert.AreEqual(_testCase.ClassContainer.Befores.First().Stage, Stage.Running);
    }

    [TestMethod]
    public void StartAfterFixture()
    {
        // Arrange
        var name = RandomUtils.GetRandomString();

        // Act
        StepManager.StartAfterFixture(name);

        // Assert
        Assert.AreEqual(_testCase.ClassContainer.Afters.First().DisplayName, name);
        Assert.AreNotEqual(_testCase.ClassContainer.Afters.First().Start, 0);
        Assert.AreEqual(_testCase.ClassContainer.Afters.First().Stage, Stage.Running);
    }

    [TestMethod]
    public void StartStep()
    {
        // Arrange
        var name = RandomUtils.GetRandomString();
        var description = RandomUtils.GetRandomString();

        // Act
        StepManager.StartStep(name, result => result.Description = description);

        // Assert
        Assert.AreEqual(_testCase.TestResult.Steps.First().Description, description);
        Assert.AreEqual(_testCase.TestResult.Steps.First().DisplayName, name);
        Assert.AreEqual(_testCase.TestResult.Steps.First().Stage, Stage.Running);
    }

    [TestMethod]
    public void PassStep()
    {
        // Arrange
        var name = RandomUtils.GetRandomString();
        var description = RandomUtils.GetRandomString();
        StepManager.StartStep(name);

        // Act
        StepManager.PassStep(result => result.Description = description);

        // Assert
        Assert.AreEqual(_testCase.TestResult.Steps.First().DisplayName, name);
        Assert.AreEqual(_testCase.TestResult.Steps.First().Stage, Stage.Finished);
        Assert.AreEqual(_testCase.TestResult.Steps.First().Status, Status.Passed);
    }

    [TestMethod]
    public void FailStep()
    {
        // Arrange
        var name = RandomUtils.GetRandomString();
        StepManager.StartStep(name);

        // Act
        StepManager.FailStep();

        // Assert
        Assert.AreEqual(_testCase.TestResult.Steps.First().DisplayName, name);
        Assert.AreEqual(_testCase.TestResult.Steps.First().Stage, Stage.Finished);
        Assert.AreEqual(_testCase.TestResult.Steps.First().Status, Status.Failed);
    }

    [TestMethod]
    public void StopFixture()
    {
        // Arrange
        var name = RandomUtils.GetRandomString();
        var description = RandomUtils.GetRandomString();
        StepManager.StartBeforeFixture(name);

        // Act
        StepManager.StopFixture(result => result.Description = description);

        // Assert
        Assert.AreEqual(_testCase.ClassContainer.Befores.First().Description, description);
        Assert.AreEqual(_testCase.ClassContainer.Befores.First().DisplayName, name);
        Assert.AreEqual(_testCase.ClassContainer.Befores.First().Stage, Stage.Finished);
        Assert.AreNotEqual(_testCase.ClassContainer.Befores.First().Stop, 0);
    }

    [TestMethod]
    public void StopFixtureSuppressTestCase()
    {
        // Arrange
        var name = RandomUtils.GetRandomString();
        StepManager.StartBeforeFixture(name);

        // Act
        StepManager.StopFixtureSuppressTestCase(result => result.Status = Status.Skipped);

        // Assert
        Assert.AreEqual(_testCase.ClassContainer.Befores.First().DisplayName, name);
        Assert.AreEqual(_testCase.ClassContainer.Befores.First().Status, Status.Skipped);
    }
}
