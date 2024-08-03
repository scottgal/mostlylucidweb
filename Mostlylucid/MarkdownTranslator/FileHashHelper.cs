using System.IO.Hashing;
using System.Text;
using System.Text.RegularExpressions;

namespace Mostlylucid.MarkdownTranslator;

public static class FileHashHelper
{
    private static readonly string InvalidChars = new(Path.GetInvalidFileNameChars());

    private static readonly Regex InvalidCharsRegex = new($"[{Regex.Escape(InvalidChars)}]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);






    private static async Task<string> ComputeHash(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        stream.Position = 0;
        var bytes = new byte[stream.Length];
        await stream.ReadAsync(bytes);
        stream.Position = 0;
        var hash = XxHash64.Hash(bytes);
        var hashString = Convert.ToBase64String(hash);
        hashString = InvalidCharsRegex.Replace(hashString, "_");
        return hashString;
    }

    public static async Task<bool> IsFileChanged(this string filePath, string outDir)
    {
        var hashFileName = Path.GetFileNameWithoutExtension(filePath) + ".hash";
        var currentHash = await ComputeHash(filePath);
        var hashFile = Path.Combine(outDir ?? string.Empty, hashFileName);
        if (!File.Exists(hashFile))
        {
            await WriteHashFile(hashFile, currentHash);
            return true;
        }

        var oldHash = await File.ReadAllTextAsync(hashFile);
        if (oldHash != currentHash)
        {
            await WriteHashFile(hashFile, currentHash);
            return true;
        }

        return false;
    }

    private static async Task WriteHashFile(string filePath, string hash)
    {
        await using var stream = File.OpenWrite(filePath);
        var bytes = Encoding.UTF8.GetBytes(hash);
        await stream.WriteAsync(bytes);
    }
}