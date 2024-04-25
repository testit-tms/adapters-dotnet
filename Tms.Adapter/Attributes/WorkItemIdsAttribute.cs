namespace Tms.Adapter.Attributes;

public class WorkItemIdsAttribute : BaseAttribute<List<string>>
{
    public WorkItemIdsAttribute(params string[] workItemIds)
    {
        Value = workItemIds.ToList();
    }
}