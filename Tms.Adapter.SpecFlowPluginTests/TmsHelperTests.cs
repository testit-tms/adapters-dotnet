using Tms.Adapter.SpecFlowPlugin;
using Tms.Adapter.SpecFlowPluginTests.Helper;
using TechTalk.SpecFlow.Bindings;
using Tms.Adapter.Core.Models;
using Tms.Adapter.Core.Utils;
using Newtonsoft.Json;

namespace Tms.Adapter.SpecFlowPluginTests
{
    [TestClass]
    public class TmsHelperTests : TestsBase
    {
        [TestMethod]
        public void GetFixtureResult()
        {
            // Arrange
            var methodInfo = typeof(TmsBindings).GetMethod("BeforeTestRun");
            var hookOrder = new Random().Next();
            var hook = CollectionHelper.GetHookBinding(methodInfo, HookType.BeforeTestRun, hookOrder);

            // Act
            var result = TmsHelper.GetFixtureResult(hook);

            // Assert
            Assert.AreEqual(result.DisplayName, $"{methodInfo?.Name} [{hookOrder}]");
        }

        [TestMethod]
        public void GetFeatureContainerId()
        {
            // Arrange
            var contextManager = CollectionHelper.GetContextManager(_specFlowConfiguration, _testTracer);
            var featureInfo = contextManager.FeatureContext?.FeatureInfo;

            // Act
            var id = TmsHelper.GetFeatureContainerId(featureInfo);

            // Assert
            Assert.AreEqual(id, featureInfo.GetHashCode().ToString());
        }

        [TestMethod]
        public void StartTestContainer()
        {
            // Arrange
            var contextManager = CollectionHelper.GetContextManager(_specFlowConfiguration, _testTracer);
            _bindingInvoker.InvokeBindingHelper(StatusBinding.FirstBeforeFeature, _testTracer, contextManager);

            // Act
            var result = TmsHelper.StartTestContainer(contextManager.FeatureContext, contextManager.ScenarioContext);

            // Assert
            Assert.AreEqual(result, contextManager.FeatureContext.Get<HashSet<ClassContainer>>().First());
            Assert.AreEqual(result, contextManager.ScenarioContext.Get<ClassContainer>());
        }

        [TestMethod]
        public void StartTestCase()
        {
            // Arrange
            var tagValueDelimiter = ",";

            var linkDictionary = new Dictionary<string, string>()
            {
                { "url", "https://test01.example"},
                { "title", RandomUtils.GetRandomString() },
                { "description", RandomUtils.GetRandomString() },
                { "type", "Issue" }
            };

            var tagsDictionary = new Dictionary<string, string>()
            {
                { "ExternalId", RandomUtils.GetRandomString() },
                { "DisplayName", RandomUtils.GetRandomString() },
                { "Title", RandomUtils.GetRandomString() },
                { "Description", RandomUtils.GetRandomString() },
                { "Labels", RandomUtils.GetRandomString() + tagValueDelimiter + RandomUtils.GetRandomString() },
                { "Links", JsonConvert.SerializeObject(linkDictionary)},
                { "WorkItemIds", new Random().Next().ToString() + tagValueDelimiter + new Random().Next().ToString() }
            };
            var tags = tagsDictionary.Select(i => i.Key + "=" + i.Value).ToArray();
            
            var contextManager = CollectionHelper.GetContextManager(_specFlowConfiguration, _testTracer, tags);

            _bindingInvoker.InvokeBindingHelper(StatusBinding.FirstBeforeFeature, _testTracer, contextManager);

            var classContainer = TmsHelper.StartTestContainer(contextManager.FeatureContext, contextManager.ScenarioContext);

            // Act
            var result = TmsHelper.StartTestCase(classContainer.Id, contextManager.FeatureContext, contextManager.ScenarioContext);

            // Assert
            Assert.IsNotNull(result.Id);
            Assert.IsNotNull(result.ExternalId);
            Assert.AreNotEqual(result.Start, 0);
            Assert.AreEqual(result.Stop, 0);

            Assert.AreEqual(result.ExternalId, tagsDictionary["ExternalId"]);
            Assert.AreEqual(result.Title, tagsDictionary["Title"]);
            Assert.AreEqual(result.DisplayName, tagsDictionary["DisplayName"]);
            Assert.AreEqual(result.Description, tagsDictionary["Description"]);

            Assert.AreEqual(result.WorkItemIds[0], tagsDictionary["WorkItemIds"].Split(tagValueDelimiter)[0]);
            Assert.AreEqual(result.WorkItemIds[1], tagsDictionary["WorkItemIds"].Split(tagValueDelimiter)[1]);

            Assert.AreEqual(result.Labels[0], tagsDictionary["Labels"].Split(tagValueDelimiter)[0]);
            Assert.AreEqual(result.Labels[1], tagsDictionary["Labels"].Split(tagValueDelimiter)[1]);

            Assert.AreEqual(result.Links[0].Url, linkDictionary["url"]);
            Assert.AreEqual(result.Links[0].Title, linkDictionary["title"]);
            Assert.AreEqual(result.Links[0].Description, linkDictionary["description"]);
            Assert.AreEqual(result.Links[0].Type, LinkType.Issue);
        }

        [TestMethod]
        public void GetCurrentTestCase()
        {
            // Arrange
            var contextManager = CollectionHelper.GetContextManager(_specFlowConfiguration, _testTracer);

            _bindingInvoker.InvokeBindingHelper(StatusBinding.FirstBeforeFeature, _testTracer, contextManager);
            var classContainer = TmsHelper.StartTestContainer(contextManager.FeatureContext, contextManager.ScenarioContext);
            var testContainer = TmsHelper.StartTestCase(classContainer.Id, contextManager.FeatureContext, contextManager.ScenarioContext);

            // Act
            var result = TmsHelper.GetCurrentTestCase(contextManager.ScenarioContext);

            // Assert
            Assert.AreEqual(result, testContainer);
        }

        [TestMethod]
        public void GetCurrentTestContainer()
        {
            // Arrange
            var contextManager = CollectionHelper.GetContextManager(_specFlowConfiguration, _testTracer);

            _bindingInvoker.InvokeBindingHelper(StatusBinding.FirstBeforeFeature, _testTracer, contextManager);
            var classContainer = TmsHelper.StartTestContainer(contextManager.FeatureContext, contextManager.ScenarioContext);
            var testContainer = TmsHelper.StartTestCase(classContainer.Id, contextManager.FeatureContext, contextManager.ScenarioContext);

            // Act
            var result = TmsHelper.GetCurrentTestContainer(contextManager.ScenarioContext);

            // Assert
            Assert.AreEqual(result, classContainer);
        }
    }
}
