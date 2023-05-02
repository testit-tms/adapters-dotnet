using Tms.Adapter.Core.Models;

namespace Tms.Adapter.Core.Writer;

public interface IWriter
{
    void Write(TestResult result);
}