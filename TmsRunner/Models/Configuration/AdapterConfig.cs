using CommandLine;

namespace TmsRunner.Models.Configuration;

public sealed class AdapterConfig
{
    [Option('r', "runner", Required = true,
        HelpText =
            "Set path to test runner. Example: --runner '/opt/homebrew/Cellar/dotnet/6.0.110/libexec/sdk/6.0.110/vstest.console.dll'")]
    public string? RunnerPath { get; set; }

    [Option('t', "testassembly", Required = true,
        HelpText = "Set path to test assembly. Example: --testassembly '/Tests/tests.dll'")]
    public string? TestAssemblyPath { get; set; }

    [Option('a', "testadapter", Required = false,
        HelpText = "Set path to test adapter. Example: --testadapter '/Tests/testsAdapter.dll'")]
    public string? TestAdapterPath { get; set; }

    [Option('l', "logger", Required = false,
        HelpText = "Set path to logger. Example: --logger '/Tests/logger.dll'")]
    public string? LoggerPath { get; set; }

    [Option("tmsLabelsOfTestsToRun", Required = false, HelpText = "Set labels of autotests to run. Example: --tmsLabelsOfTestsToRun smoke OR --tmsLabelsOfTestsToRun smoke,prod,cloud")]
    public string? TmsLabelsOfTestsToRun { get; set; }

    [Option('d', "debug", Required = false,
        HelpText = "Set debug level for logging. Example: --debug")]
    public bool IsDebug { get; set; }

    [Option("tmsUrl", Required = false, HelpText = "Set TMS host address.")]
    public string? TmsUrl { get; set; }

    [Option("tmsPrivateToken", Required = false, HelpText = "Set private token.")]
    public string? TmsPrivateToken { get; set; }

    [Option("tmsProjectId", Required = false, HelpText = "Set project id.")]
    public string? TmsProjectId { get; set; }

    [Option("tmsConfigurationId", Required = false, HelpText = "Set configuration id.")]
    public string? TmsConfigurationId { get; set; }

    [Option("tmsTestRunId", Required = false, HelpText = "Set test run id.")]
    public string? TmsTestRunId { get; set; }

    [Option("tmsTestRunName", Required = false, HelpText = "Set test run name.")]
    public string? TmsTestRunName { get; set; }

    [Option("tmsAdapterMode", Required = false, HelpText = "Set adapter mode.")]
    public string? TmsAdapterMode { get; set; }

    public string? TmsConfigFile { get; set; }

    [Option("tmsRunSettings", Required = false, HelpText = "Set run settings.")]
    public string? TmsRunSettings { get; set; }

    [Option("tmsAutomaticCreationTestCases", Required = false, HelpText = "Set automatic creation test cases.")]
    public string? TmsAutomaticCreationTestCases { get; set; }

    [Option("tmsCertValidation", Default = "true", Required = false, HelpText = "Set certificate validation.")]
    public string? TmsCertValidation { get; set; }

    public Config ToInternalConfig()
    {
        return new Config
        {
            TmsUrl = TmsUrl,
            TmsPrivateToken = TmsPrivateToken,
            TmsProjectId = TmsProjectId,
            TmsConfigurationId = TmsConfigurationId,
            TmsTestRunId = TmsTestRunId,
            TmsTestRunName = TmsTestRunName,
            TmsAdapterMode = TmsAdapterMode,
            TmsConfigFile = TmsConfigFile,
            TmsRunSettings = TmsRunSettings,
            TmsAutomaticCreationTestCases = TmsAutomaticCreationTestCases,
            TmsCertValidation = TmsCertValidation,
            TmsLabelsOfTestsToRun = TmsLabelsOfTestsToRun
        };
    }
}