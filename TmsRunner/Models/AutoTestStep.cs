namespace TmsRunner.Models;

public class AutoTestStep
{
    public string Title { get; set; }
    public string Description { get; set; }
    public List<AutoTestStep> Steps { get; set; }

    public static AutoTestStep ConvertFromStep(Step step)
    {
        return new AutoTestStep
        {
            Title = step.Title,
            Description = step.Description,
            Steps = step.Steps.Select(ConvertFromStep).ToList()
        };
    }
}