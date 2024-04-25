using Microsoft.Extensions.Configuration;
using System.Configuration;
using TmsRunner.Entities;
using TmsRunner.Entities.Configuration;
using TmsRunner.Extensions;

namespace TmsRunner.Managers;

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
            _ = configBuilder.AddJsonFile(configurationFileLocation);
        }
        else
        {
            Console.WriteLine($"Configuration file was not found at {configurationFileLocation}");
        }

        var config = configBuilder
            .Add(new EnvConfigurationSource())
            .Add(new ClassConfigurationSource(adapterConfig))
            .Build();

        var tmsSettings = new TmsSettings();
        config.Bind(tmsSettings);

        Validate(tmsSettings);

        return tmsSettings;
    }

    private static string GetConfigFileName(string? path)
    {
        var defaultConfFileName = DefaultConfigFileName;
        var envConfFileName = Environment.GetEnvironmentVariable(EnvConfigFile);
        defaultConfFileName = defaultConfFileName.AssignIfNullOrEmpty(envConfFileName);

        return defaultConfFileName.AssignIfNullOrEmpty(path);
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

        if (!string.IsNullOrWhiteSpace(settings.RunSettings) && !IsValidXml(settings.RunSettings))
        {
            throw new ConfigurationErrorsException("Run settings is invalid");
        }

        switch (settings.AdapterMode)
        {
            case 0:
                {
                    if (!Guid.TryParse(settings.TestRunId, out _))
                    {
                        throw new ConfigurationErrorsException(
                            "Adapter works in mode 0. Config should contains valid test run id.");
                    }

                    break;
                }
            case 1:
                {
                    if (!Guid.TryParse(settings.TestRunId, out _))
                    {
                        throw new ConfigurationErrorsException(
                            "Adapter works in mode 1. Config should contains valid test run id.");
                    }

                    break;
                }
            case 2:
                if (Guid.TryParse(settings.TestRunId, out _))
                {
                    throw new ConfigurationErrorsException(
                        "Adapter works in mode 2. Config should not contains test run id.");
                }

                break;
            default:
                throw new ConfigurationErrorsException($"Incorrect adapter mode: {settings.AdapterMode}");
        }
    }

    private static bool IsValidXml(string xmlStr)
    {
        try
        {
            if (string.IsNullOrEmpty(xmlStr))
            {
                return false;
            }

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