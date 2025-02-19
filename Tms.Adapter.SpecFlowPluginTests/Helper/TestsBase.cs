using BoDi;
using Moq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.BindingSkeletons;
using TechTalk.SpecFlow.Configuration;
using TechTalk.SpecFlow.ErrorHandling;
using TechTalk.SpecFlow.Tracing;
using Tms.Adapter.Core.Service;
using Tms.Adapter.SpecFlowPlugin;

namespace Tms.Adapter.SpecFlowPluginTests.Helper
{
    [TestClass]
    public abstract class TestsBase
    {
        internal readonly SpecFlowConfiguration _specFlowConfiguration;
        internal readonly TmsTestTracer _testTracer;
        internal readonly TmsBindingInvoker _bindingInvoker;

        public TestsBase()
        {
            _specFlowConfiguration = new SpecFlowConfiguration(ConfigSource.Default,
                customDependencies: new ContainerRegistrationCollection(),
                generatorCustomDependencies: new ContainerRegistrationCollection(),
                featureLanguage: new CultureInfo("en-US"),
                bindingCulture: null,
                stopAtFirstError: false,
                MissingOrPendingStepsOutcome.Pending,
                traceSuccessfulSteps: true,
                traceTimings: false,
                minTracedDuration: new TimeSpan(1000000),
                StepDefinitionSkeletonStyle.RegexAttribute,
                additionalStepAssemblies: new List<string>(),
                allowDebugGeneratedFiles: false,
                allowRowTests: true,
                addNonParallelizableMarkerForTags: [],
                ObsoleteBehavior.Warn);

            _testTracer = new TmsTestTracer(Mock.Of<ITraceListener>(),
                Mock.Of<IStepFormatter>(),
                Mock.Of<IStepDefinitionSkeletonProvider>(),
                _specFlowConfiguration);

            _bindingInvoker = new TmsBindingInvoker(_specFlowConfiguration,
                Mock.Of<IErrorProvider>(),
                new SynchronousBindingDelegateInvoker());
        }

        [TestInitialize]
        public virtual void TestSetup()
        {
            Environment.SetEnvironmentVariable("TMS_URL", "https://example.com");
            Environment.SetEnvironmentVariable("TMS_PRIVATE_TOKEN", "token");
            Environment.SetEnvironmentVariable("TMS_PROJECT_ID", Guid.NewGuid().ToString());
            Environment.SetEnvironmentVariable("TMS_CONFIGURATION_ID", Guid.NewGuid().ToString());
            Environment.SetEnvironmentVariable("TMS_TEST_RUN_ID", Guid.NewGuid().ToString());
        }
    }
}
