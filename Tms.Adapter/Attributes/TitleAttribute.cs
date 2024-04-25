namespace Tms.Adapter.Attributes;

public class TitleAttribute : BaseAttribute<string>
{
    public TitleAttribute(string title)
    {
        Value = title;
    }
}