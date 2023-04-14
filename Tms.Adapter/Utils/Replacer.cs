using System.Collections.Generic;

namespace Tms.Adapter.Utils;

public class Replacer
{
    public string ReplaceParameters(string? value, Dictionary<string, string>? parameters)
    {
        if (string.IsNullOrEmpty(value) || parameters is null) return value;

        foreach (var pair in parameters)
        {
            var key = $"{{{pair.Key}}}";
            value = value.Replace(key, pair.Value);
        }

        return value;
    }
}