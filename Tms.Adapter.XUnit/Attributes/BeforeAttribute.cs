using AspectInjector.Broker;
using Tms.Adapter.XUnit.Aspects;

namespace Tms.Adapter.XUnit.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
[Injection(typeof(StepAspect))]
public class BeforeAttribute : Attribute
{
}