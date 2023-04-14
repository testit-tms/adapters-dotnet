using Microsoft.Extensions.Configuration;
using TmsRunner.Models;

namespace TmsRunner.Configuration;

public class ClassConfigurationProvider : ConfigurationProvider
{
    private readonly Config _config;

    public ClassConfigurationProvider(Config config)
    {
        _config = config;
    }

    public override void Load()
    {
        var data = new Dictionary<string, string>
        {
            { "Url", _config.TmsUrl },
            { "PrivateToken", _config.TmsPrivateToken },
            { "ProjectId", _config.TmsProjectId },
            { "ConfigurationId", _config.TmsConfigurationId },
            { "TestRunId", _config.TmsTestRunId },
            { "TestRunName", _config.TmsTestRunName },
            { "AdapterMode", _config.TmsAdapterMode },
            { "ConfigFile", _config.TmsConfigFile },
            { "RunSettings", _config.TmsRunSettings }
        };

        Data = data
            .Where(x => !string.IsNullOrEmpty(x.Value))
            .ToDictionary(x => x.Key, x => x.Value);
    }
}