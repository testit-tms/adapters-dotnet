namespace Tms.Adapter.Core;

public class AdapterManager
{
    public static Func<string> CurrentTestIdGetter { get; set; } = () => Thread.CurrentThread.ManagedThreadId.ToString();

}