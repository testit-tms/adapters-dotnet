using Tms.Adapter.Core.Models;

namespace Tms.Adapter.Core.Writer;

public interface IWriter
{
    Task Write(TestResult result, TestResultContainer resultContainer);
    void Write(TestResultContainer resultContainer);
}