namespace Tms.Adapter.Attributes;

public class DescriptionAttribute : BaseAttribute
{
    public string Value { get; set; }

    public DescriptionAttribute(string description)
    {
        Value = description;
    }
}