using System.Security.Cryptography;
using System.Text;
using lucidRESUME.Compiler;
using lucidRESUME.JobML;

namespace LucidResumeHost;

public sealed class CareerRecordPublisher(IConfiguration configuration, IJobMlSnapshotStore snapshots,
    ILogger<CareerRecordPublisher> logger) : BackgroundService
{
    private string? _lastAttempt;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        // Publish the bundled record before accepting traffic on a fresh installation.
        await RefreshAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken)) await RefreshAsync(stoppingToken);
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var fallback = configuration["CareerRecord:DefaultPath"]!;
        var overridePath = configuration["CareerRecord:OverridePath"]!;
        var markerPath = configuration["CareerRecord:StatePath"]!;
        var sourcePath = File.Exists(overridePath) ? overridePath : fallback;
        string? hash = null;
        try
        {
            var source = await File.ReadAllTextAsync(sourcePath, cancellationToken);
            hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
            if (_lastAttempt == hash) return;
            var current = await snapshots.GetCurrentAsync(cancellationToken);
            if (current != null && File.Exists(markerPath) &&
                (await File.ReadAllTextAsync(markerPath, cancellationToken)).Trim() == hash)
            {
                _lastAttempt = hash;
                return; // Preserve a newer record published through the owner UI.
            }
            await snapshots.PublishAsync(source, cancellationToken);
            Directory.CreateDirectory(Path.GetDirectoryName(markerPath)!);
            await File.WriteAllTextAsync(markerPath + ".tmp", hash, cancellationToken);
            File.Move(markerPath + ".tmp", markerPath, overwrite: true);
            _lastAttempt = hash;
            logger.LogInformation("Published résumé source from {Path}", sourcePath);
        }
        catch (Exception exception) when (exception is JobMlPublicationException or JobMlParseException or IOException or UnauthorizedAccessException
            or FormatException or InvalidOperationException or ArgumentException)
        {
            logger.LogWarning(exception, "Cannot publish résumé source {Path}; retaining the published record", sourcePath);
            // A bad uploaded override must not prevent a first deployment from having a record.
            if (sourcePath != fallback && await snapshots.GetCurrentAsync(cancellationToken) == null)
                await snapshots.PublishAsync(await File.ReadAllTextAsync(fallback, cancellationToken), cancellationToken);
            if (exception is JobMlPublicationException or JobMlParseException or ArgumentException) _lastAttempt = hash;
        }
    }
}
