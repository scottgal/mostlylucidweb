using Mostlylucid.Blog;

public class BackgroundEFBlogUpdater(IServiceScopeFactory scopeFactory, ILogger<BackgroundEFBlogUpdater> logger)
    : IHostedService, IDisposable
{
    private Task _backgroundTask;
    private CancellationTokenSource _cancellationTokenSource = new();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting EF Blog Updater");

        // Start the background task using the internal cancellation token source
        _backgroundTask = Task.Run(() => RunBackgroundTask(_cancellationTokenSource.Token))
                              .ContinueWith(OnTaskCompleted, cancellationToken);

        return Task.CompletedTask; // Non-blocking service start
    }

    private void OnTaskCompleted(Task task)
    {
        if (task.IsCanceled)
        {
            logger.LogInformation("EF Blog Updater task was canceled.");
        }
        else if (task.IsFaulted)
        {
            logger.LogError(task.Exception, "EF Blog Updater task encountered an error.");
        }
        else
        {
            logger.LogInformation("EF Blog Updater task completed successfully.");
        }

        // Mark the background task as null to indicate it's done
        _backgroundTask = null;
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

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping EF Blog Updater");

        // Signal cancellation to the running task
        await _cancellationTokenSource.CancelAsync();

        // Wait for the background task to complete
        await Task.WhenAny(_backgroundTask, Task.Delay(Timeout.Infinite, cancellationToken));
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }
}