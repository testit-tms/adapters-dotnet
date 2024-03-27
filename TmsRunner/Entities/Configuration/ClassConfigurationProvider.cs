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
            { "AutomaticCreationTestCases", config.TmsAutomaticCreationTestCases },
            { "CertValidation", config.TmsCertValidation }
        };

        Data = data
            .Where(x => !string.IsNullOrEmpty(x.Value))
            .ToDictionary(x => x.Key, x => x.Value);
    }
}