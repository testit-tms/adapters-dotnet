using TestIT.AdaptersApi.Model;
using Tms.Adapter.Core.Client;
using Tms.Adapter.Core.Models;
using Tms.Adapter.Core.Utils;

namespace Tms.Adapter.CoreTests.Utils;

[TestClass]
public class LayerMapperTests
{
    [TestMethod]
    public void ToApiModel_ReturnsNull_WhenEmpty()
    {
        Assert.IsNull(LayerMapper.ToApiModel(null));
        Assert.IsNull(LayerMapper.ToApiModel("  "));
    }

    [TestMethod]
    public void ToApiModel_MapsNameAndRunSource()
    {
        var layer = LayerMapper.ToApiModel("API");

        Assert.IsNotNull(layer);
        Assert.AreEqual("API", layer!.Name);
        Assert.AreEqual(LayerSource.Run, layer.Source);
    }

    [TestMethod]
    public void ApplyToCreate_SetsLayerOnlyWhenPresent()
    {
        var withLayer = new AutoTestCreateApiModel(externalId: "id", name: "name");
        LayerMapper.ApplyToCreate(withLayer, "E2E");
        Assert.IsNotNull(withLayer.Layer);
        Assert.AreEqual("E2E", withLayer.Layer.Name);

        var withoutLayer = new AutoTestCreateApiModel(externalId: "id", name: "name");
        LayerMapper.ApplyToCreate(withoutLayer, null);
        Assert.IsNull(withoutLayer.Layer);
    }

    [TestMethod]
    public void ApplyToUpdate_AlwaysSetsResetLayerFalse()
    {
        var withoutLayer = new AutoTestUpdateApiModel(externalId: "id", name: "name");
        LayerMapper.ApplyToUpdate(withoutLayer, null);
        Assert.IsFalse(withoutLayer.ResetLayer);
        Assert.IsNull(withoutLayer.Layer);

        var withLayer = new AutoTestUpdateApiModel(externalId: "id", name: "name");
        LayerMapper.ApplyToUpdate(withLayer, "my-custom-layer");
        Assert.IsFalse(withLayer.ResetLayer);
        Assert.AreEqual("my-custom-layer", withLayer.Layer!.Name);
        Assert.AreEqual(LayerSource.Run, withLayer.Layer.Source);
    }

    [TestMethod]
    public void Converter_Create_IncludesLayerWhenSet()
    {
        var container = new ClassContainer();
        var test = new TestContainer
        {
            ExternalId = Guid.NewGuid().ToString(),
            DisplayName = "test",
            Layer = TestLayers.API
        };

        var model = Converter.ConvertAutoTestDtoToPostModel(test, container, Guid.NewGuid().ToString());

        Assert.AreEqual(TestLayers.API, model.Layer!.Name);
        Assert.AreEqual(LayerSource.Run, model.Layer.Source);
    }

    [TestMethod]
    public void Converter_Update_OmitsLayerButSetsResetLayerFalse_WhenNotSet()
    {
        var container = new ClassContainer();
        var test = new TestContainer
        {
            ExternalId = Guid.NewGuid().ToString(),
            DisplayName = "test"
        };

        var model = Converter.ConvertAutoTestDtoToPutModel(test, container, Guid.NewGuid().ToString());

        Assert.IsFalse(model.ResetLayer);
        Assert.IsNull(model.Layer);
    }
}
