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
        var config = new TmsSettings 
        {
            AutomaticCreationTestCases  = false,
            CertValidation = true
        };

        var defaultJsonConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, GetConfigFileName());

        if (File.Exists(defaultJsonConfigPath))
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var fileConfig = JsonSerializer.Deserialize<TmsSettings>(File.ReadAllText(defaultJsonConfigPath), options);

            if (fileConfig != null)
            {
                config = fileConfig;
            }
        }
        else 
        {
            Console.WriteLine($"Configuration file was not found at {defaultJsonConfigPath}");
        }


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
        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            settings.Url = url;
        }

        var token = Environment.GetEnvironmentVariable(TmsPrivateToken);
        if (!string.IsNullOrWhiteSpace(token))
        {
            settings.PrivateToken = token;
        }

        var projectId = Environment.GetEnvironmentVariable(TmsProjectId);
        if (Guid.TryParse(projectId, out var _))
        {
            settings.ProjectId = projectId;
        }

        var configurationId = Environment.GetEnvironmentVariable(TmsConfigurationId);
        if (Guid.TryParse(configurationId, out var _))
        {
            settings.ConfigurationId = configurationId;
        }

        var testRunId = Environment.GetEnvironmentVariable(TmsTestRunId);
        if (Guid.TryParse(testRunId, out var _))
        {
            settings.TestRunId = testRunId;
        }

        var testRunName = Environment.GetEnvironmentVariable(TmsTestRunName);
        if (!string.IsNullOrWhiteSpace(testRunName))
        {
            settings.TestRunName = testRunName;
        }
        
        var createTestCase = Environment.GetEnvironmentVariable(TmsAutomaticCreationTestCases);
        if (bool.TryParse(createTestCase, out var value) && value)
        {
            settings.AutomaticCreationTestCases = value;
        }

        if (bool.TryParse(Environment.GetEnvironmentVariable(TmsCertValidation), out var validCert) && !validCert)
        {
            settings.CertValidation = false;
        }

        return settings;
    }
}