using Tms.Adapter.Models;

namespace TmsRunner.Entities.AutoTest;

#pragma warning disable CA1051
public sealed record AutoTest
{
    public string? Namespace;
    public string? Classname;
    public List<AutoTestStep> Steps = [];
    public List<AutoTestStep>? Setup;
    public List<AutoTestStep>? Teardown;
    public string? ExternalId;
    public string? Name;
    public string? Title;
    public string? Description;
    public List<string> WorkItemIds = [];
    public List<Link>? Links = [];
    public List<string>? Labels;
    public string? MethodName;
    public string? Message;
    public bool? IsFlaky;
}