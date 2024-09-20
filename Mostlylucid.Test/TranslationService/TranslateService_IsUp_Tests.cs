using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Mostlylucid.MarkdownTranslator;

namespace Mostlylucid.Test.TranslationService;

public class TranslateService_IsUp_Tests
{
    [Fact]
    public async Task Test_Service_Up()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMarkdownTranslatorServiceCollection();

        var serviceProvider = services.BuildServiceProvider();
        var translateService = serviceProvider.GetRequiredService<IMarkdownTranslatorService>();
        bool serviceUp = await translateService.IsServiceUp(CancellationToken.None);
        Assert.True(serviceUp);
        Assert.True(translateService.IPCount == 1);
    }

    [Fact]
    public async Task Test_Service_Up_Logged()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMarkdownTranslatorServiceCollection();

        var serviceProvider = services.BuildServiceProvider();
        var translateService = serviceProvider.GetRequiredService<IMarkdownTranslatorService>();
        bool serviceUp = await translateService.IsServiceUp(CancellationToken.None);
    
        var logger = serviceProvider.GetRequiredService<ILogger<IMarkdownTranslatorService>>();
        var fakeLogger = (FakeLogger<IMarkdownTranslatorService>)logger;
       var messages = fakeLogger.Collector.GetSnapshot();
       
       Assert.Contains(messages, x =>x.Level== LogLevel.Warning && x.Message.Contains($"Service at http://{Consts.BadHost}:24080 is not available"));
    }
}