namespace TmsRunner.Models;

public class Config
{
    public string TmsUrl { get; set; }

    public string TmsPrivateToken { get; set; }

    public string TmsProjectId { get; set; }

    public string TmsConfigurationId { get; set; }

    public string TmsTestRunId { get; set; }

    public string TmsTestRunName { get; set; }

    public string TmsAdapterMode { get; set; }

    public string TmsAdapterAutoTestRerunCount { get; set; }

    public string TmsConfigFile { get; set; }

    public string TmsRunSettings { get; set; }
    
    public string TmsAutomaticCreationTestCases { get; set; }

    public string TmsCertValidation { get; set; }

    public string TmsLabelsOfTestsToRun { get; set; }
}