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
    public async Task Test_Service_Recovers_After_Failed_Ping()
    {
        // The health monitor pings every minute; one failed ping must not drop the IP for good
        var handler = new ToggleHandler { Up = false };
        var services = new ServiceCollection();
        services.AddMarkdownTranslatorServiceCollection(handler);

        var serviceProvider = services.BuildServiceProvider();
        var translateService = serviceProvider.GetRequiredService<IMarkdownTranslatorService>();

        Assert.False(await translateService.IsServiceUp(CancellationToken.None));
        Assert.Equal(0, translateService.IPCount);

        handler.Up = true;

        Assert.True(await translateService.IsServiceUp(CancellationToken.None));
        Assert.Equal(2, translateService.IPCount);
    }

    private sealed class ToggleHandler : DelegatingHandler
    {
        public bool Up { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(Up
                ? System.Net.HttpStatusCode.OK
                : System.Net.HttpStatusCode.ServiceUnavailable));
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