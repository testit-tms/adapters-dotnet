namespace Tms.Adapter.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class DisplayNameAttribute : BaseAttribute<string>
{
    public DisplayNameAttribute(string displayName)
    {
        Value = displayName;
    }
}