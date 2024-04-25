namespace Tms.Adapter.Attributes;

public class DisplayNameAttribute : BaseAttribute<string>
{
    public DisplayNameAttribute(string displayName)
    {
        Value = displayName;
    }
}