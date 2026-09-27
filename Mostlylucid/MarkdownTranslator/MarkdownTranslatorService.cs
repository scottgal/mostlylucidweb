using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Helpers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Mostlylucid.Shared.Config;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Mostlylucid.MarkdownTranslator;

public class MarkdownTranslatorService(
    TranslateServiceConfig translateServiceConfig,
    ILogger<IMarkdownTranslatorService> logger,
    HttpClient client,
    IServiceProvider serviceProvider) : IMarkdownTranslatorService
{
    private record PostRecord(
        string target_lang,
        string[] text,
        string source_lang = "en",
        bool perform_sentence_splitting = true);


    private record PostResponse(string target_lang, string[] translated, string source_lang, float translation_time);

    public int IPCount => IPs.Length;

    private string[] IPs = translateServiceConfig.IPs;

    // Resilience pipeline combining retry for 429s and circuit breaker for other failures
    private readonly ResiliencePipeline _resiliencePipeline = new ResiliencePipelineBuilder()
        // First: Retry policy for 429 (Too Many Requests) with Retry-After header support
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(ex =>
                ex.StatusCode == HttpStatusCode.TooManyRequests),
            DelayGenerator = async args =>
            {
                // Extract Retry-After from the HttpRequestException if available
                if (args.Outcome.Exception is HttpRequestException httpEx
                    && httpEx.Data.Contains("RetryAfter")
                    && httpEx.Data["RetryAfter"] is TimeSpan retryAfter)
                {
                    logger.LogWarning(
                        "Translation service returned 429 (rate limited). Retrying after {RetryAfter} seconds (attempt {Attempt}/{MaxAttempts})",
                        retryAfter.TotalSeconds, args.AttemptNumber, 3);
                    return retryAfter;
                }

                // Default exponential backoff if no Retry-After header
                var delay = TimeSpan.FromSeconds(Math.Pow(2, args.AttemptNumber));
                logger.LogWarning(
                    "Translation service returned 429 (rate limited). Retrying after {Delay} seconds (attempt {Attempt}/{MaxAttempts})",
                    delay.TotalSeconds, args.AttemptNumber, 3);
                return delay;
            },
            OnRetry = args =>
            {
                logger.LogDebug("Retrying translation request after rate limit (attempt {Attempt})", args.AttemptNumber);
                return ValueTask.CompletedTask;
            }
        })
        // Second: Circuit breaker for persistent failures (403, 503, etc.)
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 1.0, // Break after all attempts fail
            MinimumThroughput = 3, // Require 3 failures
            BreakDuration = TimeSpan.FromMinutes(2),
            ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(ex =>
                ex.StatusCode == HttpStatusCode.Forbidden ||
                ex.StatusCode == HttpStatusCode.ServiceUnavailable),
            OnOpened = args =>
            {
                logger.LogWarning(
                    "Circuit breaker opened due to translation service overload. Will retry after {Duration}",
                    args.BreakDuration);
                return ValueTask.CompletedTask;
            },
            OnClosed = args =>
            {
                logger.LogInformation("Circuit breaker reset. Translation service is available again.");
                return ValueTask.CompletedTask;
            },
            OnHalfOpened = args =>
            {
                logger.LogInformation("Circuit breaker half-open. Testing translation service...");
                return ValueTask.CompletedTask;
            }
        })
        .Build();

    public async ValueTask<bool> IsServiceUp(CancellationToken cancellationToken)
    {
        var workingIPs = new List<string>();

        // Always probe every configured IP, not just the ones that answered last time. This runs
        // on a timer, and probing only the survivors meant one failed ping removed an IP for good -
        // once the list was empty translation stayed dead until the app was restarted.
        var configuredIPs = translateServiceConfig.IPs;

        try
        {
            logger.LogDebug("Checking service status for {IPs}", string.Join(", ", configuredIPs));
            foreach (var ip in configuredIPs)
            {
                logger.LogDebug("Checking service status at {IP}", ip);
                try
                {
                    var response = await client.GetAsync($"{ip}/model_name", cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        workingIPs.Add(ip);
                    }
                    else
                    {
                        logger.LogWarning("Service at {IP} is not available", ip);
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
        return await _resiliencePipeline.ExecuteAsync(async ct =>
        {
            // Snapshot: the health check can swap the list out between reads
            var ips = IPs;
            if (ips.Length == 0)
            {
                logger.LogError("No IPs available for translation");
                throw new Exception("No IPs available for translation");
            }

            var index = (int)((uint)Interlocked.Increment(ref currentIPIndex) % (uint)ips.Length);
            var ip = ips[index];

            logger.LogInformation("Sending request to {IP}", ip);
            var postObject = new PostRecord(targetLang, elements);

            var response = await client.PostAsJsonAsync($"{ip}/translate", postObject, ct);

            // Handle rate limiting (429 Too Many Requests) with Retry-After header
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = GetRetryAfterDelay(response);
                logger.LogDebug(
                    "Translation service at {IP} returned 429 (rate limited). Retry-After: {RetryAfter} seconds",
                    ip, retryAfter.TotalSeconds);

                var exception = new HttpRequestException(
                    "Translation service is rate limited (429)",
                    null,
                    HttpStatusCode.TooManyRequests);

                // Attach retry-after to exception data so Polly can use it
                exception.Data["RetryAfter"] = retryAfter;
                throw exception;
            }

            // Handle service overload (403 Forbidden) explicitly
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                // Suppress noisy overload warnings - handled by retry logic
                logger.LogDebug("Translation service at {IP} returned 403 (overloaded)", ip);
                throw new HttpRequestException(
                    "Translation service is overloaded (403)",
                    null,
                    HttpStatusCode.Forbidden);
            }

            // Handle other non-success status codes
            if (!response.IsSuccessStatusCode)
            {
                var statusCode = response.StatusCode;
                logger.LogError("Translation service at {IP} returned {StatusCode}", ip, statusCode);
                throw new HttpRequestException(
                    $"Translation service returned {statusCode}",
                    null,
                    statusCode);
            }

            var result = await response.Content.ReadFromJsonAsync<PostResponse>(cancellationToken: ct);

            logger.LogInformation("Translation took {Time} seconds", result.translation_time);
            return result.translated;
        }, cancellationToken);
    }

    /// <summary>
    /// Extract Retry-After delay from HTTP response headers.
    /// Supports both seconds (integer) and HTTP-date formats.
    /// </summary>
    private TimeSpan GetRetryAfterDelay(HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter?.Delta != null)
        {
            return response.Headers.RetryAfter.Delta.Value;
        }

        if (response.Headers.RetryAfter?.Date != null)
        {
            var retryDate = response.Headers.RetryAfter.Date.Value;
            var delay = retryDate - DateTimeOffset.UtcNow;
            return delay > TimeSpan.Zero ? delay : TimeSpan.FromSeconds(1);
        }

        // Default to 2 seconds if no Retry-After header present
        return TimeSpan.FromSeconds(2);
    }


    public async Task<string> TranslateMarkdown(string markdown, string targetLang, CancellationToken cancellationToken,
        Activity? activity)
    {
        // IMPORTANT: Preprocess fetch tags BEFORE translation
        // This ensures fetched remote content is translated and stored in the translated markdown files
        var preprocessor = new Mostlylucid.Markdig.FetchExtension.Processors.MarkdownFetchPreprocessor(
            serviceProvider,
            logger as ILogger<Mostlylucid.Markdig.FetchExtension.Processors.MarkdownFetchPreprocessor>);
        markdown = preprocessor.Preprocess(markdown);

        activity?.SetTag("PreprocessedMarkdown", true);

        var pipeline = new MarkdownPipelineBuilder().UsePreciseSourceLocation().ConfigureNewLine(Environment.NewLine)
            .Build();
        var document = global::Markdig.Markdown.Parse(markdown, pipeline);
        var textStrings = ExtractTextStrings(document);

        // Batch by character limit instead of fixed count for better EasyNMT optimization
        var batches = BatchTextByCharLimit(textStrings);
        List<string> translatedStrings = new();

        logger.LogInformation(
            "Translating {TotalStrings} text elements in {BatchCount} batches to {Language}",
            textStrings.Count, batches.Count, targetLang);

        foreach (var batch in batches)
        {
            try
            {
                var translatedBatch = await Post(batch, targetLang, cancellationToken);
                activity?.SetTag("BatchSize", batch.Length);
                activity?.SetTag("BatchChars", batch.Sum(s => s.Length));
                activity?.SetTag("Starting time", DateTime.UtcNow);
                translatedStrings.AddRange(translatedBatch);
                activity?.SetTag("Ending time", DateTime.UtcNow);
            }
            catch (BrokenCircuitException)
            {
                logger.LogWarning("Circuit breaker is open - translation service is temporarily unavailable for {Language}", targetLang);
                activity?.SetTag("CircuitBreakerOpen", true);
                throw new TranslateException(
                    "Translation service is temporarily unavailable due to overload. Circuit breaker is open.",
                    batch);
            }
            catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.TooManyRequests)
            {
                // 429 after all retries exhausted - this indicates sustained rate limiting
                logger.LogWarning(
                    "Translation service rate limiting persists after retries for {Language}. Service may be overloaded.",
                    targetLang);
                activity?.SetTag("RateLimitExhausted", true);
                // Don't record as failure - this is rate limiting, not a service failure
                throw new TranslateException(
                    "Translation service rate limit exceeded after retries (429)",
                    batch);
            }
            catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.Forbidden)
            {
                // Suppress noisy overload warnings - handled by retry logic
                logger.LogDebug("Translation service returned 403 (overloaded) for {Language}", targetLang);
                activity?.SetTag("ServiceOverloaded", true);
                throw new TranslateException(
                    "Translation service is overloaded (403)",
                    batch);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error translating markdown to {Language}: {Message} for strings {Strings}",
                    targetLang, e.Message,
                    string.Concat(Environment.NewLine, batch));
                throw new TranslateException(e.Message, batch);
            }
        }

        ReinsertTranslatedStrings(document, translatedStrings.ToArray());
        var outString = document.ToMarkdownString();
        outString = outString.Replace("</summary>", $"</summary>{Environment.NewLine}");
        return outString;
    }

    private static readonly Regex DatetimeHiddenRegex = new(@"^<\s*datetime\s+class\s*=\s*""hidden""\s*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static bool IsDatetimeHiddenTag(HtmlInline? htmlInline)
    {
        if (htmlInline == null) return false;
        return DatetimeHiddenRegex.IsMatch(htmlInline.Tag);
    }

    private List<string> ExtractTextStrings(MarkdownDocument document)
    {
        var textStrings = new List<string>();

        foreach (var node in document.Descendants())
        {
            if (node is LiteralInline literalInline)
            {
                if (literalInline?.Parent?.FirstChild is HtmlInline htmlInline && IsDatetimeHiddenTag(htmlInline)) continue;

                var content = literalInline?.Content.ToString();
                if (content == null) continue;
                if (!IsWord(content)) continue;

                // Pass through as-is - let translation service handle the text
                logger.LogDebug("Extracting text: '{Content}' (length: {Length}, has leading space: {LeadingSpace}, has trailing space: {TrailingSpace})",
                    content, content.Length, content.StartsWith(" "), content.EndsWith(" "));
                textStrings.Add(content);
            }
        }

        return textStrings;
    }


    private bool IsWord(string text)
    {
        // Only skip image file extensions and TOC markers
        // Let translation service handle emojis, symbols, etc.
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg" };
        if (imageExtensions.Any(text.Contains)) return false;

        if (text == "TOC]") return false;

        return text.Any(char.IsLetter);
    }

    /// <summary>
    /// Batch text strings by character limit and element count to optimize for EasyNMT input constraints.
    /// This replaces the old fixed-count batching (batch size = 5) with dynamic batching
    /// based on actual content length. Text is passed through as-is to preserve formatting and spacing.
    /// </summary>
    private List<string[]> BatchTextByCharLimit(List<string> textStrings)
    {
        var batches = new List<string[]>();
        var currentBatch = new List<string>();
        var currentLength = 0;

        foreach (var text in textStrings)
        {
            var textLength = text.Length;

            // If adding this text would exceed character limit OR element limit, start a new batch
            if (currentBatch.Count > 0 &&
                (currentLength + textLength > translateServiceConfig.MaxBatchCharacters ||
                 currentBatch.Count >= translateServiceConfig.MaxSentencesPerBatch))
            {
                batches.Add(currentBatch.ToArray());
                currentBatch = new List<string>();
                currentLength = 0;
            }

            // For very long single text elements, send alone (with warning)
            if (textLength > translateServiceConfig.MaxBatchCharacters)
            {
                logger.LogWarning(
                    "Single text element exceeds max batch size ({Length} > {Max}). Sending as single item.",
                    textLength, translateServiceConfig.MaxBatchCharacters);

                if (currentBatch.Count > 0)
                {
                    batches.Add(currentBatch.ToArray());
                    currentBatch = new List<string>();
                    currentLength = 0;
                }

                batches.Add(new[] { text });
            }
            else
            {
                currentBatch.Add(text);
                currentLength += textLength;
            }
        }

        // Add remaining text elements
        if (currentBatch.Count > 0)
        {
            batches.Add(currentBatch.ToArray());
        }

        // If no batches created but we have text, create one batch
        if (batches.Count == 0 && textStrings.Count > 0)
        {
            batches.Add(textStrings.ToArray());
        }

        return batches;
    }

    private void ReinsertTranslatedStrings(MarkdownDocument document, string[] translatedStrings)
    {
        int index = 0;

        foreach (var node in document.Descendants())
        {
            if (node is LiteralInline literalInline && index < translatedStrings.Length)
            {
                if (literalInline?.Parent?.FirstChild is HtmlInline htmlInline && IsDatetimeHiddenTag(htmlInline)) continue;
                if (literalInline == null) continue;
                var content = literalInline.Content.ToString();
                if (!IsWord(content)) continue;

                var originalContent = content;
                var translatedContent = translatedStrings[index];

                // Preserve leading/trailing whitespace from original
                var leadingSpace = originalContent.Length > 0 && char.IsWhiteSpace(originalContent[0]) ? originalContent.Substring(0, 1) : "";
                var trailingSpace = originalContent.Length > 0 && char.IsWhiteSpace(originalContent[^1]) ? originalContent.Substring(originalContent.Length - 1) : "";

                // If translation service stripped spaces, restore them
                if (!string.IsNullOrEmpty(leadingSpace) && !translatedContent.StartsWith(leadingSpace))
                {
                    translatedContent = leadingSpace + translatedContent;
                    logger.LogDebug("Restored leading space to translated text");
                }
                if (!string.IsNullOrEmpty(trailingSpace) && !translatedContent.EndsWith(trailingSpace))
                {
                    translatedContent = translatedContent + trailingSpace;
                    logger.LogDebug("Restored trailing space to translated text");
                }

                logger.LogDebug("Reinserting: original='{Original}' -> translated='{Translated}'", originalContent, translatedContent);
                literalInline.Content = new StringSlice(translatedContent, NewLine.CarriageReturnLineFeed);
                index++;
            }
        }
    }
}

public class TranslateException(string message, string[] strings)
    : Exception($"Error translating markdown: {message} for strings {string.Join(", ", strings)}")
{
}