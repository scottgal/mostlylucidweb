using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Mostlylucid.MarkdownTranslator;

namespace Mostlylucid.Test.TranslationService;

public class BackgroundTranslate_Tests
{
    private (IBackgroundTranslateService backgroundTranslateService, FakeLogger<IBackgroundTranslateService> fakeLogger)
        Setup()
    {
        var services = new ServiceCollection();
        services.AddTestServices();
        var scope = services.BuildServiceProvider();
        var backgroundTranslateService = scope.GetRequiredService<IBackgroundTranslateService>();
        var log = scope.GetRequiredService<ILogger<IBackgroundTranslateService>>();
        var fakeLogger = (FakeLogger<IBackgroundTranslateService>)log;
        return (backgroundTranslateService, fakeLogger);
    }

    [Fact]
    public async Task TestBackgroundTranslate()
    {
        var (backgroundTranslateService, fakeLogger) = Setup();
        await backgroundTranslateService.StartAsync(CancellationToken.None);
        var task = backgroundTranslateService.Translate(new MarkdownTranslationModel()
            { OriginalMarkdown = "This is a test", Language = "es" });
        var result = await task;
        var completion = await result;
        await backgroundTranslateService.StopAsync(CancellationToken.None);
        Assert.True(completion.Complete);
        Assert.Equal("Esto es una prueba", completion.TranslatedMarkdown);
        var snapshot = fakeLogger.Collector.GetSnapshot();
        Assert.Contains(snapshot, x => x.Level == LogLevel.Information && x.Message.Contains("Translating  to es"));
        Assert.Contains(snapshot, x => x.Level == LogLevel.Information && x.Message.Contains("Translated to es"));
    }

    [Fact]
    public async Task TestBackgroundTranslate_Fail()
    {
        var (backgroundTranslateService, fakeLogger) = Setup();
        await backgroundTranslateService.StartAsync(CancellationToken.None);
        var task = backgroundTranslateService.Translate(new MarkdownTranslationModel()
            { OriginalMarkdown = "This is a test", Language = "xx" });
        var result = await task;
        await Assert.ThrowsAsync<Exception>(async () => await result);
        await backgroundTranslateService.StopAsync(CancellationToken.None);
        var snapshot = fakeLogger.Collector.GetSnapshot();
        Assert.Contains(snapshot, x => x.Level == LogLevel.Error && x.Message.Contains("Error translating markdown"));
        Assert.Contains(snapshot,
            x => x.Level == LogLevel.Warning && x.Message.Contains("Translation error, retrying attempt 1"));
        Assert.Contains(snapshot,
            x => x.Level == LogLevel.Warning && x.Message.Contains("Translation error, retrying attempt 2"));
        Assert.Contains(snapshot,
            x => x.Level == LogLevel.Error && x.Message.Contains("Translation failed after 3 attempts"));
    }
}