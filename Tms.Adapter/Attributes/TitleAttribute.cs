namespace Tms.Adapter.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class TitleAttribute : BaseAttribute<string>
{
    public TitleAttribute(string title)
    {
        Value = title;
    }
}