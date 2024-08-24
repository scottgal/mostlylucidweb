using Mostlylucid.Config;
using Mostlylucid.OpenSearch.Config;
using OpenSearch.Client;

namespace Mostlylucid.OpenSearch;

public static class Setup
{
    public static  void SetupOpenSearch(this IServiceCollection services, IConfiguration configuration)
    {
        
        var openSearchConfig = services.ConfigurePOCO<OpenSearchConfig>(configuration.GetSection(OpenSearchConfig.Section));
        var config = new ConnectionSettings(new Uri(openSearchConfig.Endpoint))
            .EnableHttpCompression()
            .EnableDebugMode()
            .ServerCertificateValidationCallback((sender, certificate, chain, errors) => true)
            .BasicAuthentication(openSearchConfig.Username, openSearchConfig.Password);
        services.AddSingleton<OpenSearchClient>(c => new OpenSearchClient(config));

        services.AddScoped<PostIndexer>();
        services.AddScoped<IndexService>();
        services.AddScoped<SearchService>();


    }
    
    public static async Task SetupOpenSearchIndex(this WebApplication webApplication)
    {
        using var scope = webApplication.Services.CreateScope();
        var services = scope.ServiceProvider;
        var indexService = services.GetRequiredService<PostIndexer>();
        await indexService.AddAllPostsToIndex();
    }
}