namespace Tms.Adapter.Core.Utils;

public static class Replacer
{
    public static string ReplaceParameters(string value, Dictionary<string, string> parameters)
    {
        foreach (var pair in parameters)
        {
            var key = $"{{{pair.Key}}}";
            value = value.Replace(key, pair.Value);
        }

        return value;
    }
}