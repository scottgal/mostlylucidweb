namespace Mostlylucid.Helpers;

using System;
using System.Collections.Generic;

public static class LanguageConverter
{
    private static readonly Dictionary<string, string> LanguageMap = new Dictionary<string, string>
    {
        { "es", "Español (Spanish)" },
        { "fr", "Français (French)" },
        { "de", "Deutsch (German)" },
        { "it", "Italiano (Italian)" },
        { "zh", "中文 (Chinese Simplified)" },  // Chinese (Simplified)
        { "nl", "Nederlands (Dutch)" },
        { "hi", "हिन्दी (Hindi)"  },  // Hindi
        { "ar", "العربية (Arabic)" },  // Arabic
        { "uk", "Українська (Ukranian)" },  // Ukrainian
        { "fi", "Suomi (Finnish)"  },  // Finnish
        { "sv", "Svenska (Swedish)" },  // Swedish
        { "en", "English" }
    };


    public static string ConvertCodeToLanguage(this string code)
    {
        return LanguageMap.TryGetValue(code.ToLower(), out string languageName) ? languageName : "Unknown Language";
    }
}