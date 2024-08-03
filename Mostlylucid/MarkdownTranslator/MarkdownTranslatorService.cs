using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Mostlylucid.MarkdownTranslator;

public class MarkdownTranslatorService(ILogger<MarkdownTranslatorService> logger, HttpClient client)
{
    private record PostRecord(string target_lang, string[] text, string source_lang = "en",bool perform_sentence_splitting = false);

private Random random = Random.Shared;
    
    private record PostResponse(string target_lang, string[] translated, string source_lang, float translation_time);


    private string[] IPs = new[] { "http://192.168.0.30:24080", "http://localhost:24080", "http://192.168.0.74:24080" };
    public async ValueTask<bool> IsServiceUp(CancellationToken cancellationToken)
    {
        try
        {
            var ip = IPs[random.Next(IPs.Length)];
            logger.LogInformation("Checking service status at {IP}", ip);
            var response = await client.GetAsync($"{ip}/model_name", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error checking service status");
            return false;
        }
    }
  
    
    private async Task<string[]> Post(string[] elements, string targetLang, CancellationToken cancellationToken)
    {
        try
        {
            var ip = IPs[random.Next(IPs.Length)];
            logger.LogInformation("Sendign request to {IP}", ip);
            var postObject = new PostRecord(targetLang, elements);
            var response = await client.PostAsJsonAsync($"{ip}/translate", postObject, cancellationToken);

            var phrase = response.ReasonPhrase;
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<PostResponse>(cancellationToken: cancellationToken);

            return result.translated;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error translating markdown: {Message} for strings {Strings}", e.Message, string.Concat( elements, Environment.NewLine));
            throw;
        }
    }


    public async Task<string> TranslateMarkdown(string markdown, string targetLang, CancellationToken cancellationToken)
    {
        var document = Markdig.Markdown.Parse(markdown);
        var textStrings = ExtractTextStrings(document);
        var batchSize = 20;
        var stringLength = textStrings.Count;
        List<string> translatedStrings = new();
        for (int i = 0; i < stringLength; i += batchSize)
        {
            var batch = textStrings.Skip(i).Take(batchSize).ToArray();
            translatedStrings.AddRange(await Post(batch, targetLang, cancellationToken));
        }


        ReinsertTranslatedStrings(document, translatedStrings.ToArray());
        return document.ToMarkdownString();
    }

    private List<string> ExtractTextStrings(MarkdownDocument document)
    {
        var textStrings = new List<string>();

        foreach (var node in document.Descendants())
        {
            if (node is LiteralInline literalInline)
            {
                var content = literalInline.Content.ToString();

                if (!IsWord(content)) continue;
              
                textStrings.Add(content);
            }
        }

        return textStrings;
    }


    private bool IsWord(string text)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg" };
        if (imageExtensions.Any(text.Contains)) return false;
        return text.Any(char.IsLetter);
    }

    private void ReinsertTranslatedStrings(MarkdownDocument document, string[] translatedStrings)
    {
        int index = 0;

        foreach (var node in document.Descendants())
        {
            if (node is LiteralInline literalInline && index < translatedStrings.Length)
            {
                var content = literalInline.Content.ToString();
         
                if (!IsWord(content)) continue;
                literalInline.Content = new Markdig.Helpers.StringSlice(translatedStrings[index]);
                index++;
            }
        }
    }
}