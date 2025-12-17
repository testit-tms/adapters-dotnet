using AspectInjector.Broker;
using Tms.Adapter.XUnit.Aspects;

namespace Tms.Adapter.XUnit.Attributes;

[Injection(typeof(StepAspect))]
[AttributeUsage(AttributeTargets.All)]
public class AfterAttribute : Attribute
{
}