namespace TmsRunner.Models.AutoTest;

public sealed record AutoTestStep
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public List<AutoTestStep>? Steps { get; set; }

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