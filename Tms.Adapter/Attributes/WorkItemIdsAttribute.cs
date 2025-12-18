namespace Tms.Adapter.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class WorkItemIdsAttribute : BaseAttribute<List<string>>
{
    public WorkItemIdsAttribute(params string[] workItemIds)
    {
        Value = workItemIds.ToList();
    }
}