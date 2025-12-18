namespace TmsRunner.Utils;

[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
public sealed class PerformanceSensitiveAttribute : Attribute;