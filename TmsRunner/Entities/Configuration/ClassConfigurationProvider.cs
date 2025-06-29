using Microsoft.Extensions.Configuration;

namespace TmsRunner.Entities.Configuration;

public sealed class ClassConfigurationProvider(Config config) : ConfigurationProvider
{
    public override void Load()
    {
        var data = new Dictionary<string, string?>
        {
            { "Url", config.TmsUrl },
            { "PrivateToken", config.TmsPrivateToken },
            { "ProjectId", config.TmsProjectId },
            { "ConfigurationId", config.TmsConfigurationId },
            { "TestRunId", config.TmsTestRunId },
            { "TestRunName", config.TmsTestRunName },
            { "AdapterMode", config.TmsAdapterMode },
            { "ConfigFile", config.TmsConfigFile },
            { "RunSettings", config.TmsRunSettings },
            { "IgnoreParameters", config.TmsIgnoreParameters },
            { "AutomaticCreationTestCases", config.TmsAutomaticCreationTestCases },
            { "AutomaticUpdationLinksToTestCases", config.TmsAutomaticUpdationLinksToTestCases },
            { "CertValidation", config.TmsCertValidation },
            { "RerunTestsCount", config.TmsRerunTestsCount },
            { "IsDebug", config.IsDebug.ToString() }
        };

        Data = data
            .Where(x => !string.IsNullOrEmpty(x.Value))
            .ToDictionary(x => x.Key, x => x.Value);
    }
}