namespace Tms.Adapter.Core.Attributes;

public class DisplayNameAttribute : Attribute, ITmsAttribute
{
    public string Value { get; }

    public DisplayNameAttribute(string value)
    {
        Value = value;
    }
}