using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Plugins;
using TechTalk.SpecFlow.Tracing;
using TechTalk.SpecFlow.UnitTestProvider;
using Tms.Adapter.SpecFlowPlugin;

[assembly: RuntimePlugin(typeof(TmsPlugin))]

namespace Tms.Adapter.SpecFlowPlugin;

public class TmsPlugin : IRuntimePlugin
{
    public void Initialize(RuntimePluginEvents runtimePluginEvents, RuntimePluginParameters runtimePluginParameters,
        UnitTestProviderConfiguration unitTestProviderConfiguration)
    {
        runtimePluginEvents.CustomizeGlobalDependencies += (_, args) =>
            args.ObjectContainer.RegisterTypeAs<TmsBindingInvoker, IBindingInvoker>();

        runtimePluginEvents.CustomizeTestThreadDependencies += (_, args) =>
            args.ObjectContainer.RegisterTypeAs<TmsTestTracer, ITestTracer>();
    }
}