namespace TmsRunner.Entities.Configuration;

public sealed record Config
{
    public string? TmsUrl;
    public string? TmsPrivateToken;
    public string? TmsProjectId;
    public string? TmsConfigurationId;
    public string? TmsTestRunId;
    public string? TmsTestRunName;
    public string? TmsAdapterMode;
    public string? TmsConfigFile;
    public string? TmsRunSettings;
    public string? TmsAutomaticCreationTestCases;
    public string? TmsAutomaticUpdationLinksToTestCases;
    public string? TmsCertValidation;
    public string? TmsLabelsOfTestsToRun;
    public string? TmsIgnoreParameters;
}
