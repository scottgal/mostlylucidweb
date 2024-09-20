using Microsoft.Extensions.DependencyInjection;
using Mostlylucid.MarkdownTranslator;
using Mostlylucid.Test.TranslationService.Helpers;

namespace Mostlylucid.Test.TranslationService;

public class TranslateService_Translate_Tests
{
    [Fact]
    public async Task Test_Translate_Success()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMarkdownTranslatorServiceCollection();

        var serviceProvider = services.BuildServiceProvider();
        var translateService = serviceProvider.GetRequiredService<IMarkdownTranslatorService>();
        var markdown = "This is a test";
        var targetLang = "es";
        var translated = await translateService.TranslateMarkdown(markdown, targetLang, CancellationToken.None, null);
        Assert.Equal("Esto es una prueba", translated);
    }
    
    [Fact(DisplayName = "Tests what happens when the service returns an error")]
    public async Task Test_Translate_Fail()
    {
        var services = new ServiceCollection();
        services.AddMarkdownTranslatorServiceCollection();

        var serviceProvider = services.BuildServiceProvider();
        var translateService = serviceProvider.GetRequiredService<IMarkdownTranslatorService>();
        var markdown = "This is a test";
        var targetLang = "xx";
        await Assert.ThrowsAsync<TranslateException>(async () => await translateService.TranslateMarkdown(markdown, targetLang, CancellationToken.None, null));
    }

    [Fact(DisplayName = "Tests a markdown file with elements that should not be translated")]
    public async Task Test_Translate_File()
    {
        var services = new ServiceCollection();
        services.AddMarkdownTranslatorServiceCollection();

        var serviceProvider = services.BuildServiceProvider();
        var translateService = serviceProvider.GetRequiredService<IMarkdownTranslatorService>();
        var markdown =ResourceHelper.GetMarkdownResource("Mostlylucid.Test.TranslationService.TestDocuments.elements.md");
        var targetLang = "es";
        var translated = await translateService.TranslateMarkdown(markdown, targetLang, CancellationToken.None, null);
        Assert.Equal("Esto es una prueba\n\n```csharp\nvar x = 1;\n```", translated);
    }
}