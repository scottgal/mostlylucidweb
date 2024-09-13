using Markdig;
using Markdig.Helpers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Mostlylucid.MarkdownTranslator;

public class MarkdownTranslatorService(
    TranslateServiceConfig translateServiceConfig,
    ILogger<MarkdownTranslatorService> logger,
    HttpClient client)
{
    private record PostRecord(
        string target_lang,
        string[] text,
        string source_lang = "en",
        bool perform_sentence_splitting = true);


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
                try
                {
                    var response = await client.GetAsync($"{ip}/model_name", cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        workingIPs.Add(ip);
                    }
                }
                catch (Exception)
                {
                    logger.LogWarning("Service at {IP} is not available", ip);
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
            if (!IPs.Any())
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
            logger.LogError(e, "Error translating markdown: {Message} for strings {Strings}", e.Message,
                string.Concat(elements, Environment.NewLine));
            throw;
        }
    }


    public async Task<string> TranslateMarkdown(string markdown, string targetLang, CancellationToken cancellationToken)
    {
        var pipeline = new MarkdownPipelineBuilder().UsePreciseSourceLocation().ConfigureNewLine(Environment.NewLine).Build();
        var document = Markdig.Markdown.Parse(markdown, pipeline);
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
        var outString= document.ToMarkdownString();
        outString = outString.Replace("</summary>", $"</summary>{Environment.NewLine}");
        return outString;
    }

    private List<string> ExtractTextStrings(MarkdownDocument document)
    {
        var textStrings = new List<string>();

        foreach (var node in document.Descendants())
        {
            if (node is LiteralInline literalInline)
            {
                if (literalInline?.Parent?.FirstChild is HtmlInline { Tag: "<datetime class=\"hidden\">" }) continue;
                
                var content = literalInline?.Content.ToString();
                if(content == null) continue;
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

        if (text == "TOC]") return false;
        return text.Any(char.IsLetter);
    }

    private void ReinsertTranslatedStrings(MarkdownDocument document, string[] translatedStrings)
    {
        int index = 0;

        foreach (var node in document.Descendants())
        {
            if (node is LiteralInline literalInline && index < translatedStrings.Length)
            {
                if (literalInline?.Parent?.FirstChild is HtmlInline { Tag: "<datetime class=\"hidden\">" }) continue;
                if(literalInline==null) continue;
                var content = literalInline.Content.ToString();
                if (!IsWord(content)) continue;
                var translatedContent = translatedStrings[index];
                literalInline.Content = new StringSlice(translatedContent, NewLine.CarriageReturnLineFeed);
                index++;
            }
        }
    }
    

}