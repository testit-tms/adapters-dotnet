namespace Tms.Adapter.Attributes;

public class WorkItemIdsAttribute : BaseAttribute
{
    public List<string> Value { get; set; }

    public WorkItemIdsAttribute(params string[] workItemIds)
    {
        Value = workItemIds.ToList();
    }
}