using Tms.Adapter.Utils;

namespace TmsRunnerTests.Utils;

[TestClass]
public class ReplacerTests
{
    private const string ValueWithTags = "some string {a} {b} {c}";

    private static readonly Dictionary<string, string> ParametersWithValue = new()
    {
        { "a", "first" },
        { "b", "second" },
        { "c", "third" }
    };

    [TestMethod]
    public void ReplaceParametersValueIsNull()
    {
        // Act
        var actual = Replacer.ReplaceParameters(null, ParametersWithValue);

        // Assert
        Assert.IsNull(actual);
    }

    [TestMethod]
    public void ReplaceParametersValueIsEmptyString()
    {
        // Act
        var actual = Replacer.ReplaceParameters(string.Empty, ParametersWithValue);

        // Assert
        Assert.AreEqual(string.Empty, actual);
    }

    [TestMethod]
    public void ReplaceParametersParametersIsEmpty()
    {
        // Arrange
        var parameters = new Dictionary<string, string>();

        // Act
        var actual = Replacer.ReplaceParameters(ValueWithTags, parameters);

        // Assert
        Assert.AreEqual(ValueWithTags, actual);
    }

    [TestMethod]
    public void ReplaceParametersReplaceTags()
    {
        const string excepted = "some string first second third";

        // Act
        var actual = Replacer.ReplaceParameters(ValueWithTags, ParametersWithValue);

        // Assert
        Assert.AreEqual(excepted, actual);
    }

    [TestMethod]
    public void ReplaceParametersValueWithOutTags()
    {
        const string excepted = "some string first second third";

        // Act
        var actual = Replacer.ReplaceParameters(excepted, ParametersWithValue);

        // Assert
        Assert.AreEqual(excepted, actual);
    }
}