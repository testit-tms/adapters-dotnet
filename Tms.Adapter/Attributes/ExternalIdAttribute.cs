namespace Tms.Adapter.Attributes
{
    public class ExternalIdAttribute : BaseAttribute<string>
    {
        public ExternalIdAttribute(string externalId)
        {
            Value = externalId;
        }
    }
}
