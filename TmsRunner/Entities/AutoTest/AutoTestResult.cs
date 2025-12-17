using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Tms.Adapter.Models;

namespace TmsRunner.Entities.AutoTest;

public sealed record AutoTestResult
{
    public List<Link>? Links { get; set; }
    public string? Message { get; set; }
    public string? ExternalId { get; set; }
    public TestOutcome? Outcome { get; set; }
    public string? Traces { get; set; }
    public DateTime? StartedOn { get; set; }
    public DateTime? CompletedOn { get; set; }
    public long? Duration { get; set; }
    public List<Guid>? Attachments { get; set; }
    public Dictionary<string, string>? Parameters { get; set; }
    public List<AutoTestStepResult>? StepResults { get; set; }
    public List<AutoTestStepResult>? SetupResults { get; set; }
    public List<AutoTestStepResult>? TeardownResults { get; set; }
}