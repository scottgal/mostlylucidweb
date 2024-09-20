using System.Reflection;
using Mostlylucid.Test.Tests;

namespace Mostlylucid.Test.TranslationService.Helpers;

public static class ResourceHelper
{
    
    public static string GetMarkdownResource(string resourceName)
    {
        var assembly = Assembly.GetAssembly(typeof(MarkdownParserTest));
        var resources = assembly.GetManifestResourceNames();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}