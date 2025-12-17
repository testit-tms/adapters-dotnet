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

    public static AutoTestStepResult ConvertFromStep(Step step)
    {
        return new AutoTestStepResult
        {
            Title = step.Title,
            Description = step.Description,
            Steps = step.Steps.Select(ConvertFromStep).ToList(),
            StartedOn = step.StartedOn,
            CompletedOn = step.CompletedOn,
            Duration = step.Duration,
            Attachments = step.Attachments,
            Parameters = step.Args,
            Outcome = step.Outcome
        };
    }
}