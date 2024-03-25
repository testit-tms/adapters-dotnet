using Microsoft.Extensions.Configuration;

namespace TmsRunner.Models.Configuration;

public sealed class EnvConfigurationSource : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new EnvConfigurationProvider();
    }
}