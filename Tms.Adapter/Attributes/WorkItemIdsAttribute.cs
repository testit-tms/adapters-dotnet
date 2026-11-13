namespace Tms.Adapter.Attributes;

[Obsolete("Use WorkItemId with a single globalId instead.")]
[AttributeUsage(AttributeTargets.Method)]
public class WorkItemIdsAttribute : BaseAttribute<List<string>>
{
    public WorkItemIdsAttribute(params string[] workItemIds)
    {
        Value = workItemIds.ToList();
    }
}