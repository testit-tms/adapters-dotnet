using Xunit;
using Xunit.Sdk;

namespace Tms.Adapter.XUnit.Attributes;

[XunitTestCaseDiscoverer("Tms.Adapter.XUnit.XunitDiscover", "Tms.Adapter.XUnit")]
public class TmsFactAttribute : FactAttribute
{
}