using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Mostlylucid.Shared.Config.Markdown;

namespace Mostlylucid.Resume;

/// <summary>Supervises the compiler bundled in the website image, listening on loopback only.</summary>
public sealed class EmbeddedResumeCompiler(IOptions<LucidResumeProxyOptions> configured,
    IConfiguration configuration, ILogger<EmbeddedResumeCompiler> logger) : BackgroundService
{
    public const string UpstreamUri = "http://127.0.0.1:5098/";
    public static string ExecutablePath => Path.Combine(AppContext.BaseDirectory, "resume-compiler", "LucidResumeHost.dll");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configured.Value.UseEmbeddedCompiler) return;
        var markdown = configuration.GetSection("Markdown").Get<MarkdownConfig>() ?? new MarkdownConfig();
        var site = Path.GetFullPath(Path.Combine(markdown.MarkdownPath, SiteContentService.FolderName));
        var data = Path.Combine(site, "resume-data");
        while (!stoppingToken.IsCancellationRequested)
        {
            var start = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = Path.GetDirectoryName(ExecutablePath)!,
                UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true
            };
            foreach (var argument in new[]
            {
                ExecutablePath, "--urls", UpstreamUri.TrimEnd('/'),
                $"--LucidResumeCompiler:SnapshotDirectory={Path.Combine(data, "jobml")}",
                $"--LucidResumeWeb:PublicationDirectory={Path.Combine(data, "publications")}",
                $"--CareerRecord:StatePath={Path.Combine(data, "source.sha256")}",
                $"--CareerRecord:OverridePath={Path.Combine(site, "resume-career-record.md")}",
                $"--CareerRecord:DefaultPath={Path.Combine(AppContext.BaseDirectory, "site-content", "resume-career-record.md")}",
                $"--LucidResumeCompiler:PublicBaseUri={configured.Value.PublicBaseUri ?? "https://mostlylucid.net/resume"}",
                "--LucidResumeCompiler:RequireAuthenticatedWriter=false",
                "--LucidResumeWeb:PublishCompiledResumes=true",
                "--Tailoring:ExtractionProvider=none", "--Ollama:CompositionEnabled=false", "--OpenAi:ApiKey="
            }) start.ArgumentList.Add(argument);
            using var process = new Process { StartInfo = start };
            var started = false;
            process.OutputDataReceived += (_, args) => { if (args.Data != null) LogOutput(args.Data); };
            process.ErrorDataReceived += (_, args) => { if (args.Data != null) logger.LogWarning("Résumé compiler: {Message}", args.Data); };
            try
            {
                started = process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync(stoppingToken);
                logger.LogWarning("Embedded résumé compiler exited with code {Code}; restarting", process.ExitCode);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                logger.LogError(exception, "Could not start embedded résumé compiler");
            }
            finally
            {
                try
                {
                    if (started && !process.HasExited) process.Kill(entireProcessTree: true);
                }
                catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                    logger.LogDebug(exception, "Résumé compiler exited while being stopped");
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }

    private void LogOutput(string line)
    {
        try
        {
            using var json = JsonDocument.Parse(line);
            var root = json.RootElement;
            var level = Enum.TryParse<LogLevel>(root.GetProperty("LogLevel").GetString(), out var parsed) ? parsed : LogLevel.Information;
            logger.Log(level, "Résumé compiler: {Message} {Exception}", root.GetProperty("Message").GetString(),
                root.TryGetProperty("Exception", out var exception) ? exception.GetString() : null);
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            logger.LogInformation("Résumé compiler: {Message}", line);
        }
    }
}
