namespace Tms.Adapter.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class ExternalIdAttribute : BaseAttribute<string>
{
    public ExternalIdAttribute(string externalId)
    {
        Value = externalId;
    }
}