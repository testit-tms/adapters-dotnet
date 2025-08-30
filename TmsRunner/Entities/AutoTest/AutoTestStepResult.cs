namespace TmsRunner.Entities.AutoTest;

#pragma warning disable CA1051
public sealed record AutoTestStepResult
{
    public string? Title;
    public string? Description;
    public DateTime? StartedOn;
    public DateTime? CompletedOn;
    public long? Duration;
    public List<Guid>? Attachments;
    public Dictionary<string, string>? Parameters;
    public List<AutoTestStepResult>? Steps;
    public string? Outcome;

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