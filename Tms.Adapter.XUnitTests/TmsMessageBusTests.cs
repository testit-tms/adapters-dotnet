using Xunit.Sdk;
using Tms.Adapter.XUnit;
using Xunit.Abstractions;
using Moq;
using Tms.Adapter.Core.Service;
using Tms.Adapter.Core.Models;
using Tms.Adapter.Core.Utils;
using ITestMethod = Xunit.Abstractions.ITestMethod;

namespace Tms.Adapter.XUnitTests;
#pragma warning disable CA1707
[TestClass]
public class TmsMessageBusTests
{
    private readonly TmsMessageBus _messageBus;
    private readonly TmsXunitTestCase _testCase;

    private readonly string _className;
    private readonly string _methodName;

    private static class MessageStubs
    {
        public static Mock<ITestCaseStarting> testCaseStarting = new();
        public static Mock<ITestClassConstructionFinished> testClassConstructionFinished = new();
        public static Mock<ITestFailed> testFailed = new();
        public static Mock<ITestPassed> testPassed = new();
        public static Mock<ITestCaseFinished> testCaseFinished = new();
    }

    public TmsMessageBusTests()
    {
        _className = RandomUtils.GetRandomString();
        _methodName = RandomUtils.GetRandomString();

        _messageBus = new TmsMessageBus(Mock.Of<IMessageBus>());

        _testCase = new TmsXunitTestCase(
            Mock.Of<IMessageSink>(),
            TestMethodDisplay.ClassAndMethod,
            Mock.Of<ITestMethod>(m => m.TestClass.Class.Name == _className
                && m.Method.Name == _methodName
                && m.Method.GetParameters() == new List<IParameterInfo>() { })
        );

        MessageStubs.testCaseStarting.SetupGet(message => message.TestCase).Returns(_testCase);
        MessageStubs.testClassConstructionFinished.SetupGet(message => message.TestCase).Returns(_testCase);
        MessageStubs.testFailed.SetupGet(message => message.TestCase).Returns(_testCase);
        MessageStubs.testPassed.SetupGet(message => message.TestCase).Returns(_testCase);
        MessageStubs.testCaseFinished.SetupGet(message => message.TestCase).Returns(_testCase);

        // TODO: incorrect property
        try
        {
            var displayName = _testCase.DisplayName;
        }
        catch (Exception)
        {
        }
    }

    [TestInitialize]
    public void InitializeTest()
    {
        Environment.SetEnvironmentVariable("TMS_URL", "https://example.com");
        Environment.SetEnvironmentVariable("TMS_PRIVATE_TOKEN", "token");
        Environment.SetEnvironmentVariable("TMS_PROJECT_ID", Guid.NewGuid().ToString());
        Environment.SetEnvironmentVariable("TMS_CONFIGURATION_ID", Guid.NewGuid().ToString());
        Environment.SetEnvironmentVariable("TMS_TEST_RUN_ID", Guid.NewGuid().ToString());
    }

    [TestCleanup]
    public void TestCleanup()
    {
        AdapterManager.ClearInstance();
    }

    [TestMethod]
    public void QueueMessage_StartTestContainer()
    {
        // Act
        var result = _messageBus.QueueMessage(MessageStubs.testCaseStarting.Object);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNotNull(_testCase.ClassContainer!.Id);
        Assert.AreNotEqual(_testCase.ClassContainer.Start, 0);
        Assert.AreEqual(_testCase.ClassContainer.Stop, 0);
    }

    [TestMethod]
    public void QueueMessage_StartTestCase()
    {
        // Arrange
        _messageBus.QueueMessage(MessageStubs.testCaseStarting.Object);

        // Act
        var result = _messageBus.QueueMessage(MessageStubs.testClassConstructionFinished.Object);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNotNull(_testCase.TestResult!.Id);
        Assert.IsNotNull(_testCase.TestResult.ExternalId);
        Assert.AreEqual(_testCase.TestResult.ClassName, _className);
        Assert.AreEqual(_testCase.TestResult.DisplayName, _methodName);
        Assert.AreEqual(_testCase.TestResult.Status, Status.Undefined);
        Assert.AreEqual(_testCase.TestResult.Stage, Stage.Running);
        Assert.AreNotEqual(_testCase.TestResult.Start, 0);
        Assert.AreEqual(_testCase.TestResult.Stop, 0);
        Assert.AreEqual(_testCase.ClassContainer.Children.First(), _testCase.TestResult.Id);
    }

    [TestMethod]
    public void QueueMessage_MarkTestCaseAsFailed()
    {
        // Arrange
        _messageBus.QueueMessage(MessageStubs.testCaseStarting.Object);
        _messageBus.QueueMessage(MessageStubs.testClassConstructionFinished.Object);

        var message = RandomUtils.GetRandomString();
        var trace = RandomUtils.GetRandomString();

        MessageStubs.testFailed.SetupGet(message => message.Messages).Returns([message]);
        MessageStubs.testFailed.SetupGet(message => message.StackTraces).Returns([trace]);

        // Act
        var result = _messageBus.QueueMessage(MessageStubs.testFailed.Object);

        // Assert
        Assert.IsFalse(result);
        Assert.AreEqual(_testCase.TestResult.Status, Status.Failed);
        Assert.AreEqual(_testCase.TestResult.Message, message);
        Assert.AreEqual(_testCase.TestResult.Trace, trace);
    }

    [TestMethod]
    public void QueueMessage_MarkTestCaseAsPassed()
    {
        // Arrange
        _messageBus.QueueMessage(MessageStubs.testCaseStarting.Object);
        _messageBus.QueueMessage(MessageStubs.testClassConstructionFinished.Object);

        // Act
        var result = _messageBus.QueueMessage(MessageStubs.testPassed.Object);

        // Assert
        Assert.IsFalse(result);
        Assert.AreEqual(_testCase.TestResult.Status, Status.Passed);
    }

    [TestMethod]
    public void QueueMessage_FinishTestCase()
    {
        // Arrange
        _messageBus.QueueMessage(MessageStubs.testCaseStarting.Object);
        _messageBus.QueueMessage(MessageStubs.testClassConstructionFinished.Object);
        _messageBus.QueueMessage(MessageStubs.testPassed.Object);

        // Act
        var result = _messageBus.QueueMessage(MessageStubs.testCaseFinished.Object);

        // Assert
        Assert.IsFalse(result);
        Assert.AreEqual(_testCase.TestResult.Stage, Stage.Finished);
        Assert.AreNotEqual(_testCase.TestResult.Stop, 0);
        Assert.AreNotEqual(_testCase.ClassContainer.Stop, 0);
    }
}