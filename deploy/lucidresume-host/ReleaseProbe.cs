using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LucidResumeHost;

/// <summary>Runs inside the release container, including its native PDF exporter.</summary>
internal static class ReleaseProbe
{
    public static async Task<int> RunAsync(Uri site)
    {
        try
        {
            using var client = CreateClient(site);
            var deadline = DateTime.UtcNow.AddSeconds(45);
            while (true)
            {
                try
                {
                    using var status = await client.GetAsync("/resume/api/status");
                    status.EnsureSuccessStatusCode();
                    using var json = JsonDocument.Parse(await status.Content.ReadAsStringAsync());
                    if (json.RootElement.GetProperty("ready").GetBoolean())
                    {
                        if (status.Headers.CacheControl?.NoStore != true)
                            throw new InvalidOperationException("Compiler responses must not be cached.");
                        break;
                    }
                }
                catch (HttpRequestException) when (DateTime.UtcNow < deadline) { }
                if (DateTime.UtcNow >= deadline) throw new TimeoutException("Bundled career record did not become ready.");
                await Task.Delay(1000);
            }
            foreach (var path in new[] { "/healthz", "/blog", "/resumes" })
                using (var page = await client.GetAsync(path)) page.EnsureSuccessStatusCode();
            foreach (var format in new[] { "pdf", "docx", "md" })
                await CheckDownloadAsync(client, $"/resumes/Scott-Galloway-Complete-Resume.{format}", format);

            // Warm the form with an existing CSRF cookie, then verify a separate visitor.
            // This catches accidental caching of one visitor's antiforgery token for another.
            await client.GetStringAsync("/resume");
            await CompileAsync(client);
            using var visitor = CreateClient(site);
            await CompileAsync(visitor);
            Console.WriteLine("Release check passed: bundled record, independent visitors, compilation and PDF/Word/Markdown downloads.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Release check failed: {exception.Message}");
            return 1;
        }
    }

    private static HttpClient CreateClient(Uri site) =>
        new(new HttpClientHandler { CookieContainer = new CookieContainer() })
        { BaseAddress = site, Timeout = TimeSpan.FromSeconds(30) };

    private static async Task CompileAsync(HttpClient client)
    {
        var html = await client.GetStringAsync("/resume");
        var token = Regex.Match(html, "const token='([^']+)'").Groups[1].Value;
        if (token.Length == 0) throw new InvalidOperationException("Compiler form has no antiforgery token.");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/resume/api/compile")
        {
            Content = JsonContent.Create(new
            {
                jobDescription = "Principal engineer and engineering leader with C#, ASP.NET Core, Azure, distributed systems and AI experience.",
                polish = false, includeCitations = true, publish = false, minimumPages = 2
            })
        };
        request.Headers.Add("X-CSRF-TOKEN", token);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        foreach (var download in result.RootElement.GetProperty("downloads").EnumerateObject())
            await CheckDownloadAsync(client, download.Value.GetString()!, download.Name);
    }

    private static async Task CheckDownloadAsync(HttpClient client, string path, string format)
    {
        using var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync();
        if (bytes.Length < 100 || format == "pdf" && !bytes.AsSpan().StartsWith("%PDF"u8)
            || format == "docx" && !bytes.AsSpan().StartsWith("PK"u8))
            throw new InvalidOperationException($"Invalid {format} download at {path}.");
    }
}
