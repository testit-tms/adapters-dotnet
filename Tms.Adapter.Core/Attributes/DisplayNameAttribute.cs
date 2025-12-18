namespace Tms.Adapter.Core.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Delegate)]
public class DisplayNameAttribute(string value) : Attribute, ITmsAttribute
{
    public string Value { get; } = value;
}