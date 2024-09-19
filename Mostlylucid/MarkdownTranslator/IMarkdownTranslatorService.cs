using System.Diagnostics;

namespace Mostlylucid.MarkdownTranslator;

public interface IMarkdownTranslatorService
{
    int IPCount { get; }
    ValueTask<bool> IsServiceUp(CancellationToken cancellationToken);

    Task<string> TranslateMarkdown(string markdown, string targetLang, CancellationToken cancellationToken,
        Activity? activity);
}