using Microsoft.Extensions.Caching.Memory;

namespace Mostlylucid.Services;

public class BaseControllerService(IBlogService blogService, AnalyticsSettings analyticsSettings, AuthSettings authSettings,IMemoryCache cache)
{
    public IBlogService BlogService => blogService;
    public AnalyticsSettings AnalyticsSettings => analyticsSettings;
    public AuthSettings AuthSettings => authSettings;
    
    public IMemoryCache MemoryCache=> cache;
    
}