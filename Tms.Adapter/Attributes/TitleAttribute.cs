namespace Tms.Adapter.Attributes;

public class TitleAttribute : BaseAttribute
{
    public string Value { get; set; }

    public TitleAttribute(string title)
    {
        Value = title;
    }
}