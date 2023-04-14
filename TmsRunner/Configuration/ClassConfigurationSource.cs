using Microsoft.Extensions.Configuration;
using TmsRunner.Models;

namespace TmsRunner.Configuration;

public class ClassConfigurationSource : IConfigurationSource
{
    private readonly Config _config;

    public ClassConfigurationSource(Config config)
    {
        _config = config;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new ClassConfigurationProvider(_config);
}