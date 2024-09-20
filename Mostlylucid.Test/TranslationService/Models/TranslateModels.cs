namespace Mostlylucid.Test.TranslationService.Models;

public record PostRecord(
    string target_lang,
    string[] text,
    string source_lang = "en",
    bool perform_sentence_splitting = true);


public record PostResponse(string target_lang, string[] translated, string source_lang, float translation_time);