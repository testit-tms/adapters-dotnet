using Tms.Adapter.Core.Models;

namespace Tms.Adapter.XUnit;

public interface ITestResultAccessor
{
    TestResultContainer TestResultContainer { get; set; }
    TestResult TestResult { get; set; }
}