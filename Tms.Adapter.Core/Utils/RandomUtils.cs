namespace Tms.Adapter.Core.Utils;

public static class RandomUtils
{
    private static readonly Random _random = new();

    public static string GetRandomString(int length = 10)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";

        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }
}
