using Microsoft.Extensions.Caching.Memory;
using Mostlylucid.Blog.ViewServices;
using Mostlylucid.Shared.Config;

namespace Mostlylucid.Services;

public class BaseControllerService(IBlogViewService blogViewService, AnalyticsSettings analyticsSettings, AuthSettings authSettings,IMemoryCache cache)
{
    public IBlogViewService BlogViewService => blogViewService;
    public AnalyticsSettings AnalyticsSettings => analyticsSettings;
    public AuthSettings AuthSettings => authSettings;
    
    public IMemoryCache MemoryCache=> cache;
    
}