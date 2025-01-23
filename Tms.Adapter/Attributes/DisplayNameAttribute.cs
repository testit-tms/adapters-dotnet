namespace Tms.Adapter.Attributes;

public class DisplayNameAttribute : BaseAttribute
{
    public string Value { get; set; }

    public DisplayNameAttribute(string displayName)
    {
        Value = displayName;
    }
}