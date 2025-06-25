namespace Tms.Adapter.Core.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class LabelsAttribute : Attribute, ITmsAttribute
{
    public List<string> Lables { get; }

    public LabelsAttribute(params string[] lables)
    {
        Lables = lables.ToList();
    }
}