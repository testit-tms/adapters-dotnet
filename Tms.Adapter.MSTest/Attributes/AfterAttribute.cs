using AspectInjector.Broker;
using Tms.Adapter.MSTest.Aspects;

namespace Tms.Adapter.MSTest.Attributes;

[Injection(typeof(StepAspect))]
public class AfterAttribute : Attribute
{
}