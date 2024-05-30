using Tms.Adapter.Core.Utils;

namespace Tms.Adapter.CoreTests.Utils;

[TestClass]
public class ReplacerTests
{
    [TestMethod]
    public void ReplaceParameters()
    {
        // Arrange
        const string value = "{key1}_{key2}_test";
        const string expected = "value1_value2_test";
        var parameters = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        // Act
        var actual = Replacer.ReplaceParameters(value, parameters);

        // Assert
        Assert.AreEqual(expected, actual);
    }
}