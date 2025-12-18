namespace Tms.Adapter.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class DescriptionAttribute : BaseAttribute<string>
{
    public DescriptionAttribute(string description)
    {
        Value = description;
    }
}