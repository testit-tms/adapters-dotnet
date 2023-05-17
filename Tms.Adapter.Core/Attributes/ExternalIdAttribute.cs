namespace Tms.Adapter.Core.Attributes;

public class ExternalIdAttribute : Attribute, ITmsAttribute
{
    public string Value { get; }

    public ExternalIdAttribute(string value)
    {
        Value = value;
    }
}