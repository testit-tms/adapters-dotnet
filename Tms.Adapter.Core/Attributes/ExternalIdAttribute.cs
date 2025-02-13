namespace Tms.Adapter.Core.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class ExternalIdAttribute : Attribute, ITmsAttribute
{
    public string Value { get; }

    public ExternalIdAttribute(string value)
    {
        Value = value;
    }
}