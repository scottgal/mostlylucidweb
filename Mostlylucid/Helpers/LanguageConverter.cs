namespace Mostlylucid.Helpers;

using System;
using System.Collections.Generic;

public static class LanguageConverter
{
    private static readonly Dictionary<string, string> LanguageMap = new Dictionary<string, string>
    {
        { "es", "Español" },
        { "fr", "Français" },
        { "de", "Deutsch" },
        { "it", "Italiano" },
        { "zh", "中文" },  // Chinese (Simplified)
        { "nl", "Nederlands" },
        { "hi", "हिन्दी" },  // Hindi
        { "ar", "العربية" },  // Arabic
        { "uk", "Українська" },  // Ukrainian
        { "fi", "Suomi" },  // Finnish
        { "sv", "Svenska" },  // Swedish
        { "en", "English" }
    };


    public static string ConvertCodeToLanguage(this string code)
    {
        return LanguageMap.TryGetValue(code.ToLower(), out string languageName) ? languageName : "Unknown Language";
    }
}