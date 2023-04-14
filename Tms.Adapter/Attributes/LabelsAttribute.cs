using System.Collections.Generic;
using System.Linq;

namespace Tms.Adapter.Attributes
{
    public class LabelsAttribute : BaseAttribute<List<string>>
    {
        public LabelsAttribute(params string[] labels)
        {
            Value = labels.ToList();
        }
    }
}