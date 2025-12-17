namespace Tms.Adapter.Core.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Delegate)]
public class TitleAttribute : Attribute, ITmsAttribute
{
    public string Value { get; }

    public TitleAttribute(string value)
    {
        Value = value;
    }
}