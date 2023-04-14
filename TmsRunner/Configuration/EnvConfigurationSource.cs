using Microsoft.Extensions.Configuration;

namespace TmsRunner.Configuration;

public class EnvConfigurationSource : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new EnvConfigurationProvider();
}