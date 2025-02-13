using System.Configuration;

using Newtonsoft.Json;

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
    private const string TmsAutomaticUpdationLinksToTestCases = "TMS_AUTOMATIC_UPDATION_LINKS_TO_TEST_CASES";
    private const string TmsCertValidation = "TMS_CERT_VALIDATION";
    private const string ConfigFile = "TMS_CONFIG_FILE";
    private const string TmsIgnoreParameters = "TMS_IGNORE_PARAMETERS";

    public static TmsSettings GetConfig()
    {
        var config = new TmsSettings 
        {
            AutomaticCreationTestCases  = false,
            AutomaticUpdationLinksToTestCases = false,
            CertValidation = true
        };

        var defaultJsonConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, GetConfigFileName());

        if (File.Exists(defaultJsonConfigPath))
        {
            var fileConfig = JsonConvert.DeserializeObject<TmsSettings>(File.ReadAllText(defaultJsonConfigPath));

            if (fileConfig != null)
            {
                config = fileConfig;
            }
        }
        else 
        {
            Console.WriteLine($"Configuration file was not found at {defaultJsonConfigPath}");
        }

        ApplyEnv(config);
        Validate(config);

        return config;
    }

    private static string GetConfigFileName()
    {
        var envConfigFileName = Environment.GetEnvironmentVariable(ConfigFile);

        return envConfigFileName ?? DefaultFileName;
    }

    private static void ApplyEnv(TmsSettings settings)
    {
        var url = Environment.GetEnvironmentVariable(TmsUrl);
        if (!string.IsNullOrWhiteSpace(url) && Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            settings.Url = url;
        }

        var token = Environment.GetEnvironmentVariable(TmsPrivateToken);
        if (!string.IsNullOrWhiteSpace(token))
        {
            settings.PrivateToken = token;
        }

        var projectId = Environment.GetEnvironmentVariable(TmsProjectId);
        if (!string.IsNullOrWhiteSpace(projectId) && Guid.TryParse(projectId, out _))
        {
            settings.ProjectId = projectId;
        }

        var configurationId = Environment.GetEnvironmentVariable(TmsConfigurationId);
        if (!string.IsNullOrWhiteSpace(configurationId) && Guid.TryParse(configurationId, out _))
        {
            settings.ConfigurationId = configurationId;
        }

        var testRunId = Environment.GetEnvironmentVariable(TmsTestRunId);
        if (!string.IsNullOrWhiteSpace(testRunId) && Guid.TryParse(testRunId, out _))
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

        var updateLinksToTestCase = Environment.GetEnvironmentVariable(TmsAutomaticUpdationLinksToTestCases);
        if (bool.TryParse(updateLinksToTestCase, out var updateLinks) && updateLinks)
        {
            settings.AutomaticUpdationLinksToTestCases = updateLinks;
        }

        if (bool.TryParse(Environment.GetEnvironmentVariable(TmsCertValidation), out var validCert) && !validCert)
        {
            settings.CertValidation = false;
        }

        if (bool.TryParse(Environment.GetEnvironmentVariable(TmsIgnoreParameters),
                out var ignoreParams) && ignoreParams)
        {
            settings.IgnoreParameters = true;
        }
    }

    private static void Validate(TmsSettings settings)
    {

        if (!Uri.IsWellFormedUriString(settings.Url, UriKind.Absolute))
        {
            throw new ConfigurationErrorsException("Url is invalid");
        }

        if (string.IsNullOrWhiteSpace(settings.PrivateToken))
        {
            throw new ConfigurationErrorsException("Private token is invalid");
        }

        if (!Guid.TryParse(settings.ProjectId, out _))
        {
            throw new ConfigurationErrorsException("Project id is invalid");
        }

        if (!Guid.TryParse(settings.ConfigurationId, out _))
        {
            throw new ConfigurationErrorsException("Configuration id is invalid");
        }

        if (string.IsNullOrWhiteSpace(settings.TestRunId))
        {
            return;
        }

        if (!Guid.TryParse(settings.TestRunId, out _))
        {
            throw new ConfigurationErrorsException(
                "Config contains not valid test run id.");
        }
    }
}