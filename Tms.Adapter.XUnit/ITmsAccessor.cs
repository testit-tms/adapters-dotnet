using Tms.Adapter.Core.Models;

namespace Tms.Adapter.XUnit;

public interface ITmsAccessor
{
    ClassContainer? ClassContainer { get; set; }
    TestContainer? TestResult { get; set; }
}