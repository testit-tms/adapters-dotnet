namespace TmsRunner.Entities.Configuration;

public sealed record Config
{
    public string? TmsUrl { get; init; }
    public string? TmsPrivateToken { get; init; }
    public string? TmsProjectId { get; init; }
    public string? TmsConfigurationId { get; init; }
    public string? TmsTestRunId { get; init; }
    public string? TmsTestRunName { get; init; }
    public string? TmsAdapterMode { get; init; }
    public string? TmsConfigFile { get; init; }
    public string? TmsRunSettings { get; init; }
    public string? TmsAutomaticCreationTestCases { get; init; }
    public string? TmsAutomaticUpdationLinksToTestCases { get; init; }
    public string? TmsCertValidation { get; init; }
    public string? TmsLabelsOfTestsToRun { get; init; }
    public string? TmsIgnoreParameters { get; init; }
    public string? TmsRerunTestsCount { get; init; }
    public bool IsDebug { get; init; }
}
