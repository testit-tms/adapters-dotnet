namespace Tms.Adapter.Core.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Delegate)]
public class LabelsAttribute(params string[] lables) : Attribute, ITmsAttribute
{
    public List<string> Lables { get; } = lables.ToList();
}