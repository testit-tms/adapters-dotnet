using Tms.Adapter.SpecFlowPluginTests.Helper;
using Tms.Adapter.Core.Models;

namespace Tms.Adapter.SpecFlowPluginTests;

#pragma warning disable CA1707

[TestClass]
public class TmsBindingInvokerTests : TestsBase
{
    [TestMethod]
    public void BeforeFeature_Success()
    {
        // Arrange
        var contextManager = CollectionHelper.GetContextManager(_specFlowConfiguration, _testTracer);

        // Act
        _bindingInvoker.InvokeBindingHelper(StatusBinding.FirstBeforeFeature, _testTracer, contextManager);

        // Assert
        Assert.AreEqual(contextManager.FeatureContext.Count, 2);
    }

    [TestMethod]
    public void BeforeScenario_FirstOrder_Success()
    {
        // Arrange
        var contextManager = CollectionHelper.GetContextManager(_specFlowConfiguration, _testTracer);
        _bindingInvoker.InvokeBindingHelper(StatusBinding.FirstBeforeFeature, _testTracer, contextManager);

        // Act
        _bindingInvoker.InvokeBindingHelper(StatusBinding.FirstBeforeScenario, _testTracer, contextManager);

        // Assert
        var classContainer = contextManager.FeatureContext.Get<HashSet<ClassContainer>>().First();

        Assert.AreEqual(contextManager.ScenarioContext.Count, 1);
        Assert.AreEqual(classContainer, contextManager.ScenarioContext.Get<ClassContainer>());

        Assert.IsNotNull(classContainer.Id);
        Assert.AreNotEqual(classContainer.Start, 0);
        Assert.AreEqual(classContainer.Stop, 0);
    }

    [TestMethod]
    public void BeforeScenario_LastOrder_Success()
    {
        // Arrange
        var contextManager = CollectionHelper.GetContextManager(_specFlowConfiguration, _testTracer);
        _bindingInvoker.InvokeBindingHelper(StatusBinding.FirstBeforeFeature, _testTracer, contextManager);
        _bindingInvoker.InvokeBindingHelper(StatusBinding.FirstBeforeScenario, _testTracer, contextManager);

        // Act
        _bindingInvoker.InvokeBindingHelper(StatusBinding.LastBeforeScenario, _testTracer, contextManager);

        // Assert
        var classContainer = contextManager.FeatureContext.Get<HashSet<ClassContainer>>().First();
        var testContainer = contextManager.FeatureContext.Get<HashSet<TestContainer>>().First();

        Assert.AreEqual(classContainer, contextManager.ScenarioContext.Get<ClassContainer>());
        Assert.AreEqual(testContainer, contextManager.ScenarioContext.Get<TestContainer>());

        Assert.IsNotNull(classContainer.Id);
        Assert.AreNotEqual(classContainer.Start, 0);
        Assert.AreEqual(classContainer.Stop, 0);
        Assert.AreEqual(classContainer.Children.First(), testContainer.Id);

        Assert.IsNotNull(testContainer.ExternalId);
        Assert.AreNotEqual(testContainer.Start, 0);
        Assert.AreEqual(testContainer.Stop, 0);
        Assert.AreEqual(testContainer.Status, Status.Undefined);
        Assert.AreEqual(testContainer.Stage, Stage.Running);
    }

    [TestMethod]
    public void AfterScenario_FirstOrder_Success()
    {
        // Arrange
        var contextManager = CollectionHelper.GetContextManager(_specFlowConfiguration, _testTracer);
        _bindingInvoker.InvokeBindingHelper(StatusBinding.FirstBeforeFeature, _testTracer, contextManager);
        _bindingInvoker.InvokeBindingHelper(StatusBinding.FirstBeforeScenario, _testTracer, contextManager);
        _bindingInvoker.InvokeBindingHelper(StatusBinding.LastBeforeScenario, _testTracer, contextManager);

        // Act
        _bindingInvoker.InvokeBindingHelper(StatusBinding.FirstAfterScenario, _testTracer, contextManager);

        // Assert
        var classContainer = contextManager.FeatureContext.Get<HashSet<ClassContainer>>().First();
        var testContainer = contextManager.FeatureContext.Get<HashSet<TestContainer>>().First();

        Assert.AreEqual(classContainer, contextManager.ScenarioContext.Get<ClassContainer>());
        Assert.AreEqual(testContainer, contextManager.ScenarioContext.Get<TestContainer>());

        Assert.IsNotNull(classContainer.Id);
        Assert.AreNotEqual(classContainer.Start, 0);
        Assert.AreEqual(classContainer.Stop, 0);
        Assert.AreEqual(classContainer.Children.First(), testContainer.Id);

        Assert.IsNotNull(testContainer.ExternalId);
        Assert.AreNotEqual(testContainer.Start, 0);
        Assert.AreNotEqual(testContainer.Stop, 0);
        Assert.AreEqual(testContainer.Status, Status.Passed);
        Assert.AreEqual(testContainer.Stage, Stage.Finished);
    }

    [TestMethod]
    public void AfterScenario_LastOrder_Success()
    {
        // Arrange
        var contextManager = CollectionHelper.GetContextManager(_specFlowConfiguration, _testTracer);
        _bindingInvoker.InvokeBindingHelper(StatusBinding.FirstBeforeFeature, _testTracer, contextManager);
        _bindingInvoker.InvokeBindingHelper(StatusBinding.FirstBeforeScenario, _testTracer, contextManager);
        _bindingInvoker.InvokeBindingHelper(StatusBinding.LastBeforeScenario, _testTracer, contextManager);
        _bindingInvoker.InvokeBindingHelper(StatusBinding.FirstAfterScenario, _testTracer, contextManager);

        // Act
        _bindingInvoker.InvokeBindingHelper(StatusBinding.LastAfterFeature, _testTracer, contextManager);

        // Assert
        var classContainer = contextManager.FeatureContext.Get<HashSet<ClassContainer>>().First();
        var testContainer = contextManager.FeatureContext.Get<HashSet<TestContainer>>().First();

        Assert.AreEqual(classContainer, contextManager.ScenarioContext.Get<ClassContainer>());
        Assert.AreEqual(testContainer, contextManager.ScenarioContext.Get<TestContainer>());

        Assert.IsNotNull(classContainer.Id);
        Assert.AreNotEqual(classContainer.Start, 0);
        Assert.AreNotEqual(classContainer.Stop, 0);
        Assert.AreEqual(classContainer.Children.First(), testContainer.Id);

        Assert.IsNotNull(testContainer.ExternalId);
        Assert.AreNotEqual(testContainer.Start, 0);
        Assert.AreNotEqual(testContainer.Stop, 0);
        Assert.AreEqual(testContainer.Status, Status.Passed);
        Assert.AreEqual(testContainer.Stage, Stage.Finished);
    }
}
