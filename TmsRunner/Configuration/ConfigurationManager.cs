using System.Configuration;
using Microsoft.Extensions.Configuration;
using TmsRunner.Extensions;
using TmsRunner.Models;

namespace TmsRunner.Configuration
{
    public static class ConfigurationManager
    {
        private const string EnvConfigFile = "TMS_CONFIG_FILE";
        private const string DefaultConfigFileName = "Tms.config.json";

        public static TmsSettings Configure(Config adapterConfig, string pathToConfFile)
        {
            if (string.IsNullOrWhiteSpace(pathToConfFile))
            {
                throw new ArgumentException("The path of config directory is empty", nameof(pathToConfFile));
            }

            var configFileName = GetConfigFileName(adapterConfig.TmsConfigFile);
            var configurationFileLocation = Path.Combine(pathToConfFile, configFileName);

            var configBuilder = new ConfigurationBuilder();

            if (File.Exists(configurationFileLocation))
            {
                configBuilder.AddJsonFile(configurationFileLocation);
            }
            else
            {
                Console.WriteLine($"Configuration file was not found at {configurationFileLocation}");
            }

            configBuilder.AddCustomConfiguration(adapterConfig);
            var config = configBuilder.Build();
            var testItSettings = new TmsSettings();
            config.Bind(testItSettings);

            Validate(testItSettings);

            return testItSettings;
        }

        private static string GetConfigFileName(string path)
        {
            var defaultConfFileName = DefaultConfigFileName;
            var envConfFileName = Environment.GetEnvironmentVariable(EnvConfigFile);
            defaultConfFileName = defaultConfFileName.AssignIfNullOrEmpty(envConfFileName);
            return defaultConfFileName.AssignIfNullOrEmpty(path);
        }

        private static void Validate(TmsSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Url))
            {
                throw new ConfigurationErrorsException("Url is empty");
            }

            if (string.IsNullOrWhiteSpace(settings.PrivateToken))
            {
                throw new ConfigurationErrorsException("Private token is empty");
            }

            if (string.IsNullOrWhiteSpace(settings.ConfigurationId))
            {
                throw new ConfigurationErrorsException("Configuration id is empty");
            }

            if (!string.IsNullOrWhiteSpace(settings.RunSettings) && !IsValidXML(settings.RunSettings))
            {
                throw new ConfigurationErrorsException("Run settings is invalid");
            }

            switch (settings.AdapterMode)
            {
                case 0:
                case 1:
                {
                    if (string.IsNullOrWhiteSpace(settings.TestRunId))
                    {
                        throw new ConfigurationErrorsException(
                            "Adapter works in mode 0 or 1. Config should contains test run id and configuration id.");
                    }

                    break;
                }
                case 2:
                    if (string.IsNullOrWhiteSpace(settings.ProjectId) || !string.IsNullOrWhiteSpace(settings.TestRunId))
                    {
                        throw new ConfigurationErrorsException(
                            "Adapter works in mode 2. Config should contains project id and configuration id. Also doesn't contains test run id.");
                    }

                    break;
                default:
                    throw new Exception($"Incorrect adapter mode: {settings.AdapterMode}");
            }
        }

        private static bool IsValidXML(string xmlStr)
        {
            try
            {
                if (string.IsNullOrEmpty(xmlStr)) return false;

                var xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.LoadXml(xmlStr);
                return true;
            }
            catch (System.Xml.XmlException)
            {
                return false;
            }
        }
    }
}