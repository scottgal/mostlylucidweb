using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mostlylucid.Services.Interfaces;
using Serilog;
using SerilogTracing;

namespace Mostlylucid.Services.Blog;

public class EFBlogUpdater(IServiceScopeFactory scopeFactory, ILogger<EFBlogUpdater> logger)
{


    public async Task TriggerUpdate(CancellationToken cancellationToken)
    {
        
        using var activity = Log.Logger.StartActivity("Background DB Update");
        // Start the background task using the internal cancellation token source
        await RunBackgroundTask(cancellationToken);

        activity?.Complete();
        return;
        
    }
    
  
    private async Task RunBackgroundTask(CancellationToken token)
    {
        try
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<IBlogPopulator>();
                await context.Populate(token);
            }

            logger.LogInformation("EF Blog Updater Finished");
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("EF Blog Updater was cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while updating the EF Blog");
        }
    }

}