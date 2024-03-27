using Microsoft.Extensions.Configuration;

namespace TmsRunner.Entities.Configuration;

public sealed class ClassConfigurationSource(Config config) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new ClassConfigurationProvider(config);
    }
}