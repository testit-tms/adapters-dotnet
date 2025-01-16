namespace Tms.Adapter.Core.Utils;

public static class Hash
{
    public static string GetStringSha256Hash(string text)
    {
        using var sha = new System.Security.Cryptography.SHA256Managed();
        var textData = System.Text.Encoding.UTF8.GetBytes(text);
        var hash = sha.ComputeHash(textData);
        return BitConverter.ToString(hash).Replace("-", String.Empty);
    }

    public static string NewId()
    {
        return Guid.NewGuid().ToString("N");
    }
}