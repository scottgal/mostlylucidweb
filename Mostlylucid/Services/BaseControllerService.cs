namespace Mostlylucid.Services;

public class BaseControllerService(IBlogService blogService, AnalyticsSettings analyticsSettings, AuthSettings authSettings)
{
    public IBlogService BlogService => blogService;
    public AnalyticsSettings AnalyticsSettings => analyticsSettings;
    public AuthSettings AuthSettings => authSettings;
    
}