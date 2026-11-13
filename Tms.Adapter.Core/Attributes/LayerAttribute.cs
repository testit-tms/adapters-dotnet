namespace Tms.Adapter.Core.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class LayerAttribute(string value) : Attribute, ITmsAttribute
{
    public string Value { get; } = value;
}
