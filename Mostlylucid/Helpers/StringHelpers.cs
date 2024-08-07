using System.Security.Cryptography;
using System.Text;

namespace Mostlylucid.Helpers;

public static class StringHelpers
{
    public static string TruncateAtWord(this string value, int length)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= length)
            return value;

        int endIndex = value.LastIndexOf(' ', length);
        return endIndex > 0 ? value.Substring(0, endIndex) : value.Substring(0, length);
    }
    
    public static Guid ToGuid(this string input)
    {
        using var provider = MD5.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = provider.ComputeHash(inputBytes);
        return new Guid(hashBytes);
    }

}