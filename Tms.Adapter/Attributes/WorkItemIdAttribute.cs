namespace Tms.Adapter.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class WorkItemIdAttribute : BaseAttribute<string>
{
    public WorkItemIdAttribute(string globalId)
    {
        Value = globalId;
    }
}
