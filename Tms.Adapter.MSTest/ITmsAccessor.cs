using Tms.Adapter.Core.Models;

namespace Tms.Adapter.MSTest;

public interface ITmsAccessor
{
    ClassContainer ClassContainer { get; set; }
    TestContainer TestResult { get; set; }
}