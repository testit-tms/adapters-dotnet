using TestIT.AdaptersApi.Model;

namespace Tms.Adapter.Core.Utils;

public static class LayerMapper
{
    public static LayerApiModel? ToApiModel(string? layer) =>
        string.IsNullOrWhiteSpace(layer)
            ? null
            : new LayerApiModel(name: layer.Trim(), source: LayerSource.Run);

    public static void ApplyToCreate(AutoTestCreateApiModel model, string? layer)
    {
        var apiLayer = ToApiModel(layer);
        if (apiLayer != null)
        {
            model.Layer = apiLayer;
        }
    }

    public static void ApplyToUpdate(AutoTestUpdateApiModel model, string? layer)
    {
        model.ResetLayer = false;
        var apiLayer = ToApiModel(layer);
        if (apiLayer != null)
        {
            model.Layer = apiLayer;
        }
    }
}
