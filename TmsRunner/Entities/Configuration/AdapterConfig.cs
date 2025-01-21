using CommandLine;

namespace TmsRunner.Entities.Configuration;

public sealed class AdapterConfig
{
    private readonly string? _testAdapterPath;
    private readonly string? _testAssemblyPath;
    private readonly string? _runnerPath;
    private readonly string? _loggerPath;

    [Option('r', "runner", Required = true, HelpText = "Set path to test runner. Example: --runner '/opt/homebrew/Cellar/dotnet/6.0.110/libexec/sdk/6.0.110/vstest.console.dll'")]
    public string? RunnerPath
    {
        get => GetValidFilePath(_runnerPath);
        init => _runnerPath = value;
    }

    [Option('t', "testassembly", Required = true, HelpText = "Set path to test assembly. Example: --testassembly '/Tests/tests.dll'")]
    public string? TestAssemblyPath
    {
        get => GetValidFilePath(_testAssemblyPath);
        init => _testAssemblyPath = value;
    }

    [Option('a', "testadapter", Required = false, HelpText = "Set path to test adapter. Example: --testadapter '/Tests/testsAdapter.dll'")]
    public string? TestAdapterPath
    {
        get => GetValidFilePath(_testAdapterPath);
        init => _testAdapterPath = value;
    }

    [Option('l', "logger", Required = false, HelpText = "Set path to logger. Example: --logger '/Tests/logger.dll'")]
    public string? LoggerPath
    {
        get => GetValidFilePath(_loggerPath);
        init => _loggerPath = value;
    }

    [Option("tmsLabelsOfTestsToRun", Required = false, HelpText = "Set labels of autotests to run. Example: --tmsLabelsOfTestsToRun smoke OR --tmsLabelsOfTestsToRun smoke,prod,cloud")]
    public string? TmsLabelsOfTestsToRun { get; init; }

    [Option('d', "debug", Required = false, HelpText = "Set debug level for logging. Example: --debug")]
    public bool IsDebug { get; init; }

    [Option("tmsUrl", Required = false, HelpText = "Set TMS host address.")]
    public string? TmsUrl { get; init; }

    [Option("tmsPrivateToken", Required = false, HelpText = "Set private token.")]
    public string? TmsPrivateToken { get; init; }

    [Option("tmsProjectId", Required = false, HelpText = "Set project id.")]
    public string? TmsProjectId { get; init; }

    [Option("tmsConfigurationId", Required = false, HelpText = "Set configuration id.")]
    public string? TmsConfigurationId { get; init; }

    [Option("tmsTestRunId", Required = false, HelpText = "Set test run id.")]
    public string? TmsTestRunId { get; init; }

    [Option("tmsTestRunName", Required = false, HelpText = "Set test run name.")]
    public string? TmsTestRunName { get; init; }

    [Option("tmsAdapterMode", Required = false, HelpText = "Set adapter mode.")]
    public string? TmsAdapterMode { get; init; }

    public string? TmsConfigFile { get; init; }

    [Option("tmsRunSettings", Required = false, HelpText = "Set run settings.")]
    public string? TmsRunSettings { get; set; }

    [Option("tmsAutomaticCreationTestCases", Required = false, HelpText = "Set automatic creation test cases.")]
    public string? TmsAutomaticCreationTestCases { get; init; }

    [Option("TmsAutomaticUpdationLinksToTestCases", Required = false, HelpText = "Set automatic updation links to test cases.")]
    public string? TmsAutomaticUpdationLinksToTestCases { get; init; }

    [Option("tmsCertValidation", Default = "true", Required = false, HelpText = "Set certificate validation.")]
    public string? TmsCertValidation { get; init; }

    [Option("tmsIgnoreParameters", Default = "false", Required = false, HelpText = "Set ignore parameters.")]
    public string? TmsIgnoreParameters { get; init; }

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
            TmsAutomaticUpdationLinksToTestCases = TmsAutomaticUpdationLinksToTestCases,
            TmsCertValidation = TmsCertValidation,
            TmsLabelsOfTestsToRun = TmsLabelsOfTestsToRun,
            TmsIgnoreParameters = TmsIgnoreParameters

        };
    }

    private static string? GetValidFilePath(string? relativeOrAbsoluteFilePath)
    {
        var validFilePath = string.IsNullOrWhiteSpace(Path.GetDirectoryName((relativeOrAbsoluteFilePath))) && !string.IsNullOrWhiteSpace(relativeOrAbsoluteFilePath)
            ? Path.Combine(Directory.GetCurrentDirectory(), relativeOrAbsoluteFilePath ?? string.Empty)
            : relativeOrAbsoluteFilePath;

        return validFilePath;
    }
}