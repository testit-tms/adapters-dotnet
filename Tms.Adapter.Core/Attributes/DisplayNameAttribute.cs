namespace Tms.Adapter.Core.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class DisplayNameAttribute : Attribute, ITmsAttribute
{
    public string Value { get; }

    public DisplayNameAttribute(string value)
    {
        Value = value;
    }
}