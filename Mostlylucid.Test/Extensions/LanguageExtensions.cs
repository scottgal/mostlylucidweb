using Mostlylucid.Shared.Entities;

namespace Mostlylucid.Test.Extensions;

public class LanguageExtensions
{
    private static readonly string[] Languages =
    {
        "en",
        "es",
        "fr",
        "de",
        "it",
        "zh",
        "nl",
        "hi",
        "ar",
        "uk",
        "fi",
        "sv"
    };

    public static LanguageEntity GetLanguageEntity(string name)
    {
        var langs = Languages;
        var index = Array.IndexOf(langs, name);
        return new LanguageEntity
        {
            Id = index,
            Name = name
        };
    }

    public static List<LanguageEntity> GetLanguageEntities()
    {
        var langs = Languages;

        return langs.Select((x, i) => new LanguageEntity
        {
            Id = i,
            Name = x
        }).ToList();
    }

    public static List<LanguageEntity> GetLanguageEntities(int count)
    {
        var langs = Languages.Take(count);
        return langs.Select((x, i) => new LanguageEntity
        {
            Id = i,
            Name = x
        }).ToList();
    }
}