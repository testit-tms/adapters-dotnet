namespace Tms.Adapter.Core.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Delegate)]
public class TagsAttribute(params string[] tags) : Attribute, ITmsAttribute
{
    public List<string> Tags { get; } = tags.ToList();
}