namespace Mostlylucid.MarkdownTranslator;

public interface IBackgroundTranslateService : IHostedService
{
    bool TranslationServiceUp { get; set; }
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    Task<bool> Ping(CancellationToken cancellationToken);
    Task<Task<TaskCompletion>> Translate(MarkdownTranslationModel message);

    Task<List<Task<TaskCompletion>>> TranslateForAllLanguages(
        PageTranslationModel message);

    Task TranslateAllFilesAsync();
} 