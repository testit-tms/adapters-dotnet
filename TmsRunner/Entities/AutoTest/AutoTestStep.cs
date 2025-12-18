namespace TmsRunner.Entities.AutoTest;

public sealed record AutoTestStep
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public List<AutoTestStep>? Steps { get; init; }

    public static AutoTestStep ConvertFromStep(StepModel stepModel)
    {
        return new AutoTestStep
        {
            Title = stepModel.Title ?? string.Empty,
            Description = stepModel.Description ?? string.Empty,
            Steps = stepModel.Steps.Select(ConvertFromStep).ToList()
        };
    }
}