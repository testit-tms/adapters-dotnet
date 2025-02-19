using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tms.Adapter.SpecFlowPluginTests.Helper
{
    public enum StatusBinding
    {
        FirstBeforeFeature,
        FirstBeforeScenario,
        LastBeforeScenario,
        FirstAfterScenario,
        LastAfterFeature
    }
}
