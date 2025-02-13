namespace TmsRunner.Entities.AutoTest;

#pragma warning disable CA1051
public sealed record AutoTestStep
{
    public string? Title;
    public string? Description;
    public List<AutoTestStep>? Steps;

    public static AutoTestStep ConvertFromStep(Step step)
    {
        return new AutoTestStep
        {
            Title = step.Title ?? string.Empty,
            Description = step.Description ?? string.Empty,
            Steps = step.Steps.Select(ConvertFromStep).ToList()
        };
    }
}