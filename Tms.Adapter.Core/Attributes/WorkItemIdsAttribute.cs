namespace Tms.Adapter.Core.Attributes;

[Obsolete("Use WorkItemId with a single globalId instead.")]
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Delegate)]
public class WorkItemIdsAttribute(params string[] ids) : Attribute, ITmsAttribute
{
    public List<string> Ids { get; } = ids.ToList();
}