using System.Text.Json;

namespace Tms.Adapter.Core.Configurator;

public static class Configurator
{
    private const string DefaultFileName = "Tms.config.json";
    private const string EnvTmsUrl = "TMS_URL";
    private const string EnvTmsPrivateToken = "TMS_PRIVATE_TOKEN";
    private const string EnvTmsProjectId = "TMS_PROJECT_ID";
    private const string EnvTmsConfigurationId = "TMS_CONFIGURATION_ID";
    private const string EnvTmsTestRunId = "TMS_TEST_RUN_ID";
    private const string EnvTmsAutomaticCreationTestCases = "TMS_AUTOMATIC_CREATION_TEST_CASES";
    private const string EnvTmsCertValidation = "TMS_CERT_VALIDATION";
    private const string EnvConfigFile = "TMS_CONFIG_FILE";

    public static TmsSettings GetConfig()
    {
        var defaultJsonConfigPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, GetConfigFileName());

        if (File.Exists(defaultJsonConfigPath))
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var config = JsonSerializer.Deserialize<TmsSettings>(File.ReadAllText(defaultJsonConfigPath), options);

            return ApplyEnv(config);
        }

        throw new Exception("Can not found config path");
    }

    private static string GetConfigFileName()
    {
        var envConfigFileName = Environment.GetEnvironmentVariable(EnvConfigFile);

        return envConfigFileName ?? DefaultFileName;
    }

    private static TmsSettings ApplyEnv(TmsSettings settings)
    {
        var url = Environment.GetEnvironmentVariable(EnvTmsUrl);
        if (url != null)
        {
            settings.Url = url;
        }

        var token = Environment.GetEnvironmentVariable(EnvTmsPrivateToken);
        if (token != null)
        {
            settings.PrivateToken = token;
        }

        var projectId = Environment.GetEnvironmentVariable(EnvTmsProjectId);
        if (projectId != null)
        {
            settings.ProjectId = projectId;
        }

        var configId = Environment.GetEnvironmentVariable(EnvTmsConfigurationId);
        if (configId != null)
        {
            settings.ConfigurationId = configId;
        }

        var testRunId = Environment.GetEnvironmentVariable(EnvTmsTestRunId);
        if (testRunId != null)
        {
            settings.TestRunId = testRunId;
        }

        var createTestCase = Environment.GetEnvironmentVariable(EnvTmsAutomaticCreationTestCases);
        if (createTestCase != null)
        {
            settings.AutomaticCreationTestCases = bool.Parse(createTestCase);
        }

        var validCert = Environment.GetEnvironmentVariable(EnvTmsCertValidation);
        if (validCert != null)
        {
            settings.CertValidation = bool.Parse(validCert);
        }

        return settings;
    }
}