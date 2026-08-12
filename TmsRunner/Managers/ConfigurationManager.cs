using Microsoft.Extensions.Configuration;
using System.Configuration;
using System.Xml;
using Tms.Adapter.Core.Utils;
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
        ApplyRawTestRunMetadata(tmsSettings, config);

        Validate(tmsSettings);

        return tmsSettings;
    }

    private static void ApplyRawTestRunMetadata(TmsSettings settings, IConfiguration config)
    {
        var tagsRaw = config["TestRunTagsRaw"];
        if (!string.IsNullOrWhiteSpace(tagsRaw))
        {
            settings.TestRunTags = TestRunMetadata.ParseTags(tagsRaw);
        }
        else if (settings.TestRunTags is { Count: > 0 })
        {
            settings.TestRunTags = settings.TestRunTags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        // JSON file may bind array; env/CLI pass JSON string via TestRunLinksRaw
        var linksRaw = config["TestRunLinksRaw"];
        if (!string.IsNullOrWhiteSpace(linksRaw))
        {
            settings.TestRunLinks = TestRunMetadata.ParseLinks(linksRaw);
        }
        else if (settings.TestRunLinks is { Count: > 0 })
        {
            settings.TestRunLinks = settings.TestRunLinks
                .Where(l => !string.IsNullOrWhiteSpace(l?.Url))
                .ToList();
        }

        // Comma-separated / JSON tags from a string JSON property if binder left list empty
        var tagsAsString = config["TestRunTags"];
        if (settings.TestRunTags.Count == 0
            && !string.IsNullOrWhiteSpace(tagsAsString)
            && (tagsAsString.Contains(',') || tagsAsString.StartsWith('[')))
        {
            settings.TestRunTags = TestRunMetadata.ParseTags(tagsAsString);
        }
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
        if (string.IsNullOrEmpty(xmlStr))
        {
            return false;
        }

        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlStr);
            return true;
        }
        catch (XmlException ex)
        {
            throw new XmlException($"RunSettings XML is invalid. " +
                                   $"Error: {ex.Message} " +
                                   $"at Line: {ex.LineNumber}, " +
                                   $"Position: {ex.LinePosition}");
        }
    }
}