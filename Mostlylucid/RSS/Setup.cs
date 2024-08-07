namespace Mostlylucid.RSS;

public static class Setup
{
    public static void SetupRSS(this IServiceCollection services)
    {
        services.AddScoped<RSSFeedService>();
        services.AddHttpContextAccessor();
    }
    
}