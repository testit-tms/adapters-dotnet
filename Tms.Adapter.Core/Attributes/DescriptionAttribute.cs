namespace Tms.Adapter.Core.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class DescriptionAttribute : Attribute, ITmsAttribute
{
    public string Value { get; }

    public DescriptionAttribute(string value)
    {
        Value = value;
    }
}