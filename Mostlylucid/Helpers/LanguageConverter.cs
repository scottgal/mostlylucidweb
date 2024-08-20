namespace Mostlylucid.Helpers;

using System;
using System.Collections.Generic;

public static class LanguageConverter
{
    private static readonly Dictionary<string, string> LanguageMap = new Dictionary<string, string>
    {
        { "es", "Spanish" },
        { "fr", "French" },
        { "de", "German" },
        { "it", "Italian" },
        { "zh", "Chinese" },
        { "nl", "Dutch" },
        { "hi", "Hindi" },
        { "ar", "Arabic" },
        { "uk", "Ukrainian" },
        { "fi", "Finnish" },
        { "sv", "Swedish" },
        {"en", "English"}
    };

    public static string ConvertCodeToLanguage(this string code)
    {
        return LanguageMap.TryGetValue(code.ToLower(), out string languageName) ? languageName : "Unknown Language";
    }
}