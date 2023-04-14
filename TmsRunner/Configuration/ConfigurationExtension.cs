using Microsoft.Extensions.Configuration;
using TmsRunner.Models;

namespace TmsRunner.Configuration;

public static class ConfigurationExtension
{
    public static void AddCustomConfiguration(this IConfigurationBuilder builder,
        Config config)
    {
        builder.Add(new EnvConfigurationSource());
        builder.Add(new ClassConfigurationSource(config));
    }
}