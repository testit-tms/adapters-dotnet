namespace Tms.Adapter.Core.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Delegate)]
public class WorkItemIdAttribute(string globalId) : Attribute, ITmsAttribute
{
    public string Value { get; } = globalId;
}
