using Tms.Adapter.Utils;

namespace TmsRunnerTests.Utils;

[TestClass]
public class ReplacerTests
{
    private const string valueWithTags = "some string {a} {b} {c}";

    private Dictionary<string, string> parametersWithValue = new()
    {
        { "a", "first" },
        { "b", "second" },
        { "c", "third" }
    };

    [TestMethod]
    public void ReplaceParameters_ValueIsNull()
    {
        var replacer = new Replacer();

        var value = replacer.ReplaceParameters(null, parametersWithValue);
        
        Assert.IsNull(value);
    }
    
    [TestMethod]
    public void ReplaceParameters_ValueIsEmptyString()
    {
        var replacer = new Replacer();

        var value = replacer.ReplaceParameters(string.Empty, parametersWithValue);
        
        Assert.AreEqual(string.Empty, value);
    }
    
    [TestMethod]
    public void ReplaceParameters_ParametersIsEmpty()
    {
        var replacer = new Replacer();
        var parameters = new Dictionary<string, string>();

        var value = replacer.ReplaceParameters(valueWithTags, parameters);
        
        Assert.AreEqual(valueWithTags, value);
    }
    
    [TestMethod]
    public void ReplaceParameters_ReplaceTags()
    {
        var replacer = new Replacer();
        const string exceptedValue = "some string first second third";

        var value = replacer.ReplaceParameters(valueWithTags, parametersWithValue);
        
        Assert.AreEqual(exceptedValue, value);
    }
    
    [TestMethod]
    public void ReplaceParameters_ValueWithOutTags()
    {
        var replacer = new Replacer();
        const string exceptedValue = "some string first second third";

        var value = replacer.ReplaceParameters(exceptedValue, parametersWithValue);
        
        Assert.AreEqual(exceptedValue, value);
    }
}