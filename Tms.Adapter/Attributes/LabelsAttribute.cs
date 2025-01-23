namespace Tms.Adapter.Attributes;

public class LabelsAttribute : BaseAttribute
{
    public List<string> Value { get; set; }

    public LabelsAttribute(params string[] labels)
    {
        Value = labels.ToList();
    }
}