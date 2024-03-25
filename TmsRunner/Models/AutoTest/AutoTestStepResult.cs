namespace TmsRunner.Models.AutoTest;

public sealed record AutoTestStepResult
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? StartedOn { get; set; }
    public DateTime? CompletedOn { get; set; }
    public long? Duration { get; set; }
    public List<Guid>? Attachments { get; set; }
    public Dictionary<string, string>? Parameters { get; set; }
    public List<AutoTestStepResult>? Steps { get; set; }
    public string? Outcome { get; set; }

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