using Xunit;
using Xunit.Sdk;

namespace Tms.Adapter.XUnit.Attributes;

[XunitTestCaseDiscoverer("Tms.Adapter.XUnit.XunitTheoryDiscover", "Tms.Adapter.XUnit")]
public class TmsTheoryAttribute : FactAttribute
{
}