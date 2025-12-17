namespace TmsRunner.Entities.AutoTest;

public sealed record AutoTestStep
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public List<AutoTestStep>? Steps { get; init; }

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