using System.Collections.Generic;
using System.Text.RegularExpressions;

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

    public string ReplaceParameters(string value, string displayName)
    {
        if (string.IsNullOrEmpty(value) || displayName is null) return value;

        var parameters = Regex.Match(displayName, @"(?<=\().*(?=\))").Value.Split(',');

        var matches = Regex.Matches(value, @"{[^{}]+}");

        for (int i = 0; i < matches.Count; i++)
        {
            value = value.Replace(matches[i].Value, parameters[i].Trim());
        }

        return value;
    }
}
