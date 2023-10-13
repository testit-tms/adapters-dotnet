using System.Security.Cryptography;
using System.Text;

namespace TmsRunner.Extensions
{
    public static class StringExtension
    {
        public static string AssignIfNullOrEmpty(this string str, string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? str : value;
        }

        public static string ComputeHash(this string str)
        {
            var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(str));
            
            return BitConverter.ToUInt32(hash).ToString();
        }

        public static string RemoveQuotes(this string str)
        {
            return string.IsNullOrWhiteSpace(str) ? str : str.Replace("'", "").Replace("\"", "");
        }
    }
}