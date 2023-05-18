using Tms.Adapter.Core.Models;

namespace Tms.Adapter.Core.Writer;

public interface IWriter
{
    Task Write(TestContainer result, ClassContainer resultContainer);
    void Write(ClassContainer resultContainer);
}