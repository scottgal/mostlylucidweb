using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Mostlylucid.Config;

namespace Mostlylucid.MarkdownTranslator;

public class MarkdownTranslatorService(TranslateServiceConfig translateServiceConfig, ILogger<MarkdownTranslatorService> logger, HttpClient client)
{
    private record PostRecord(string target_lang, string[] text, string source_lang = "en",bool perform_sentence_splitting = true);
    
    
    private record PostResponse(string target_lang, string[] translated, string source_lang, float translation_time);

    public int IPCount => IPs.Length;

    private string[] IPs = translateServiceConfig.IPs;
    public async ValueTask<bool> IsServiceUp(CancellationToken cancellationToken)
    {
        var workingIPs = new List<string>();

        try
        {
            foreach (var ip in IPs)
            {
                logger.LogInformation("Checking service status at {IP}", ip);
                var response = await client.GetAsync($"{ip}/model_name", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    workingIPs.Add(ip);
                }
            }

            IPs = workingIPs.ToArray();
            if (!IPs.Any()) return false;
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error checking service status");
            return false;
        }
    }

    private int currentIPIndex = 0;
    private async Task<string[]> Post(string[] elements, string targetLang, CancellationToken cancellationToken)
    {
        try
        {
            if(!IPs.Any())
            {
                logger.LogError("No IPs available for translation");
                throw new Exception("No IPs available for translation");
            }
            var ip = IPs[currentIPIndex];
            
            logger.LogInformation("Sending request to {IP}", ip);
        
            // Update the index for the next request
            currentIPIndex = (currentIPIndex + 1) % IPs.Length;
            var postObject = new PostRecord(targetLang, elements);
            var response = await client.PostAsJsonAsync($"{ip}/translate", postObject, cancellationToken);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<PostResponse>(cancellationToken: cancellationToken);

            logger.LogInformation("Translation took {Time} seconds", result.translation_time);
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
        var batchSize = 5;
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