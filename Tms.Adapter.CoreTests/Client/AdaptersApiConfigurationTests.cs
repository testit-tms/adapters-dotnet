using Tms.Adapter.Core.Client;

namespace Tms.Adapter.CoreTests.Client;

[TestClass]
public class AdaptersApiConfigurationTests
{
    [TestMethod]
    public void NormalizeBaseUrl_StripsApiSuffixes()
    {
        Assert.AreEqual("https://tms.example.com",
            AdaptersApiConfiguration.NormalizeBaseUrl("https://tms.example.com/api/adapters"));
        Assert.AreEqual("https://tms.example.com",
            AdaptersApiConfiguration.NormalizeBaseUrl("https://tms.example.com/api/v2"));
        Assert.AreEqual("https://tms.example.com",
            AdaptersApiConfiguration.NormalizeBaseUrl("https://tms.example.com/api"));
    }
}
