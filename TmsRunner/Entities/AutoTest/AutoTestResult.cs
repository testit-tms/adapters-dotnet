using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Tms.Adapter.Models;

namespace TmsRunner.Entities.AutoTest;

public sealed record AutoTestResult
{
    public List<Link>? Links;
    public string? Message;
    public string? ExternalId;
    public TestOutcome? Outcome;
    public string? Traces;
    public DateTime? StartedOn;
    public DateTime? CompletedOn;
    public long? Duration;
    public List<Guid>? Attachments;
    public Dictionary<string, string>? Parameters;
    public List<AutoTestStepResult>? StepResults;
    public List<AutoTestStepResult>? SetupResults;
    public List<AutoTestStepResult>? TeardownResults;
}