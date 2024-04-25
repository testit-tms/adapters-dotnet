namespace Tms.Adapter.Models;

public class StepDto
{
    public Guid Guid { get; set; }
    public DateTime? StartedOn { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Instance { get; set; }
    public string? CurrentMethod { get; set; }
    public string? CallerMethod { get; set; }
    public Dictionary<string, string>? Args { get; set; }
    public CallerMethodType? CallerMethodType { get; set; }
    public CallerMethodType? CurrentMethodType { get; set; }
}