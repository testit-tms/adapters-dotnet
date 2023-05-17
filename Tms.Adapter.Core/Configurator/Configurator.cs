using System.Text.Json;
using TmsRunner.Models;

namespace Tms.Adapter.Core.Configurator;

public static class Configurator
{
    private const string DefaultFileName = "Tms.config.json";

    public static TmsSettings GetConfig()
    {
        var defaultJsonConfigPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultFileName);

        if (File.Exists(defaultJsonConfigPath))
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<TmsSettings>(File.ReadAllText(defaultJsonConfigPath), options);
        }

        throw new Exception("Can not found config path");
    }
}