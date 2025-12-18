namespace Tms.Adapter.Core.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Delegate)]
public class WorkItemIdsAttribute(params string[] ids) : Attribute, ITmsAttribute
{
    public List<string> Ids { get; } = ids.ToList();
}