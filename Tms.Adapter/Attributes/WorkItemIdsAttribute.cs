using System.Collections.Generic;
using System.Linq;

namespace Tms.Adapter.Attributes
{
    public class WorkItemIdsAttribute : BaseAttribute<List<string>>
    {
        public WorkItemIdsAttribute(params string[] workItemIds)
        {
            Value = workItemIds.ToList();
        }
    }
}
