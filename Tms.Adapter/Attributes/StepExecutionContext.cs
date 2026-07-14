namespace Tms.Adapter.Attributes;

internal static class StepExecutionContext
{
    private static readonly AsyncLocal<StepInvocation?> CurrentInvocation = new();

    public static StepInvocation? Current
    {
        get => CurrentInvocation.Value;
        set => CurrentInvocation.Value = value;
    }
}