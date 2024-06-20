using AspectInjector.Broker;
using Tms.Adapter.MSTest.Aspects;

namespace Tms.Adapter.MSTest.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
[Injection(typeof(StepAspect))]
public class BeforeAttribute : Attribute
{
}