using Microsoft.Extensions.Configuration;

namespace TmsRunner.Configuration;

public class EnvConfigurationProvider : ConfigurationProvider
{
    private const string EnvTmsUrl = "TMS_URL";
    private const string EnvTmsPrivateToken = "TMS_PRIVATE_TOKEN";
    private const string EnvTmsProjectId = "TMS_PROJECT_ID";
    private const string EnvTmsConfigurationId = "TMS_CONFIGURATION_ID";
    private const string EnvTmsTestRunId = "TMS_TEST_RUN_ID";
    private const string EnvTmsTestRunName = "TMS_TEST_RUN_NAME";
    private const string EnvTmsAdapterMode = "TMS_ADAPTER_MODE";
    private const string EnvTmsRunSettings = "TMS_RUN_SETTINGS";
    private const string EnvTmsAutomaticCreationTestCases = "TMS_AUTOMATIC_CREATION_TEST_CASES";
    private const string EnvTmsCertValidation = "TMS_CERT_VALIDATION";

    public override void Load()
    {
        var data = new Dictionary<string, string>
        {
            { "Url", Environment.GetEnvironmentVariable(EnvTmsUrl) },
            { "PrivateToken", Environment.GetEnvironmentVariable(EnvTmsPrivateToken) },
            { "ProjectId", Environment.GetEnvironmentVariable(EnvTmsProjectId) },
            { "ConfigurationId", Environment.GetEnvironmentVariable(EnvTmsConfigurationId) },
            { "TestRunId", Environment.GetEnvironmentVariable(EnvTmsTestRunId) },
            { "TestRunName", Environment.GetEnvironmentVariable(EnvTmsTestRunName) },
            { "AdapterMode", Environment.GetEnvironmentVariable(EnvTmsAdapterMode) },
            { "RunSettings", Environment.GetEnvironmentVariable(EnvTmsRunSettings) },
            { "AutomaticCreationTestCases", Environment.GetEnvironmentVariable(EnvTmsAutomaticCreationTestCases) },
            { "CertValidation", Environment.GetEnvironmentVariable(EnvTmsCertValidation) },
        };

        Data = data
            .Where(x => !string.IsNullOrEmpty(x.Value))
            .ToDictionary(x => x.Key, x => x.Value);
    }
}