namespace TmsRunner.Entities.AutoTest;

public sealed record AutoTestStepResult
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public DateTime? StartedOn { get; init; }
    public DateTime? CompletedOn { get; init; }
    public long? Duration { get; init; }
    public List<Guid>? Attachments { get; init; }
    public Dictionary<string, string>? Parameters { get; init; }
    public List<AutoTestStepResult>? Steps { get; init; }
    public string? Outcome { get; init; }

    public static AutoTestStepResult ConvertFromStep(StepModel stepModel)
    {
        return new AutoTestStepResult
        {
            Title = stepModel.Title,
            Description = stepModel.Description,
            Steps = stepModel.Steps.Select(ConvertFromStep).ToList(),
            StartedOn = stepModel.StartedOn,
            CompletedOn = stepModel.CompletedOn,
            Duration = stepModel.Duration,
            Attachments = stepModel.Attachments,
            Parameters = stepModel.Args,
            Outcome = stepModel.Outcome
        };
    }
}