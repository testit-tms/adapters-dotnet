using System.Text.Json;

namespace Tms.Adapter.Core.Configurator;

public static class Configurator
{
    private const string DefaultFileName = "Tms.config.json";
    private const string TmsUrl = "TMS_URL";
    private const string TmsPrivateToken = "TMS_PRIVATE_TOKEN";
    private const string TmsProjectId = "TMS_PROJECT_ID";
    private const string TmsConfigurationId = "TMS_CONFIGURATION_ID";
    private const string TmsTestRunId = "TMS_TEST_RUN_ID";
    private const string TmsTestRunName = "TMS_TEST_RUN_NAME";
    private const string TmsAutomaticCreationTestCases = "TMS_AUTOMATIC_CREATION_TEST_CASES";
    private const string TmsCertValidation = "TMS_CERT_VALIDATION";
    private const string ConfigFile = "TMS_CONFIG_FILE";

    public static TmsSettings GetConfig()
    {
        var defaultJsonConfigPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, GetConfigFileName());

        if (!File.Exists(defaultJsonConfigPath))
            throw new Exception("Can not found config path");

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var config = JsonSerializer.Deserialize<TmsSettings>(File.ReadAllText(defaultJsonConfigPath), options);

        return ApplyEnv(config);
    }

    private static string GetConfigFileName()
    {
        var envConfigFileName = Environment.GetEnvironmentVariable(ConfigFile);

        return envConfigFileName ?? DefaultFileName;
    }

    private static TmsSettings ApplyEnv(TmsSettings settings)
    {
        var url = Environment.GetEnvironmentVariable(TmsUrl);
        if (!string.IsNullOrWhiteSpace(url))
        {
            settings.Url = url;
        }

        var token = Environment.GetEnvironmentVariable(TmsPrivateToken);
        if (!string.IsNullOrWhiteSpace(token))
        {
            settings.PrivateToken = token;
        }

        var projectId = Environment.GetEnvironmentVariable(TmsProjectId);
        if (!string.IsNullOrWhiteSpace(projectId))
        {
            settings.ProjectId = projectId;
        }

        var configId = Environment.GetEnvironmentVariable(TmsConfigurationId);
        if (!string.IsNullOrWhiteSpace(configId))
        {
            settings.ConfigurationId = configId;
        }

        var testRunId = Environment.GetEnvironmentVariable(TmsTestRunId);
        if (!string.IsNullOrWhiteSpace(testRunId))
        {
            settings.TestRunId = testRunId;
        }

        var testRunName = Environment.GetEnvironmentVariable(TmsTestRunName);
        if (!string.IsNullOrWhiteSpace(testRunName))
        {
            settings.TestRunName = testRunName;
        }
        
        var createTestCase = Environment.GetEnvironmentVariable(TmsAutomaticCreationTestCases);
        if (!string.IsNullOrWhiteSpace(createTestCase))
        {
            settings.AutomaticCreationTestCases = bool.Parse(createTestCase);
        }

        var validCert = Environment.GetEnvironmentVariable(TmsCertValidation);
        if (!string.IsNullOrWhiteSpace(validCert))
        {
            settings.CertValidation = bool.Parse(validCert);
        }

        return settings;
    }
}