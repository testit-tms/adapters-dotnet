namespace Tms.Adapter.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class LabelsAttribute : BaseAttribute<List<string>>
{
    public LabelsAttribute(params string[] labels)
    {
        Value = labels.ToList();
    }
}