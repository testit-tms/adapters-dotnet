namespace Tms.Adapter.Attributes;

public class ExternalIdAttribute : BaseAttribute
{
    public string Value { get; set; }

    public ExternalIdAttribute(string externalId)
    {
        Value = externalId;
    }
}