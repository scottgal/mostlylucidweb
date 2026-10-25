using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Mostlylucid.Relevance.ExperimentDemo;

/// <summary>Optional local-model producers. Frozen content/readings keep reruns independent of later model changes.</summary>
public static class EmailModels
{
    /// <summary>Captures actual decision-model readings for every exact generated email with the local model digest.</summary>
    public static async Task CaptureAsync(string model, string output)
    {
        if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any()) throw new ArgumentException("Use a new output directory.");
        Directory.CreateDirectory(output);
        using var client = Client();
        using var tags = JsonDocument.Parse(await client.GetStringAsync("api/tags"));
        var tag = tags.RootElement.GetProperty("models").EnumerateArray().Single(m => m.GetProperty("name").GetString() == (model.Contains(':') ? model : model + ":latest"));
        var digest = tag.GetProperty("digest").GetString()!;
        var version = "rubric-v1;digest-" + digest;
        var emails = new List<CharacterisedEmail>();
        foreach (var draft in EmailVariants.Generate())
        {
            var structure = EmailCharacterisation.Analyse(draft.Subject, draft.Html);
            var readings = await EmailCharacterisation.DecideAsync(client, model, version, structure);
            emails.Add(EmailVariants.Characterise(draft, readings));
            Console.WriteLine($"DECISION {emails.Count:00} delivery={readings.DeliveryExplanation:F4} help={readings.OffersHelp:F4}");
        }
        var json = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
        await File.WriteAllTextAsync(Path.Combine(output, "characterisations.json"), JsonSerializer.Serialize(emails, json));
        await File.WriteAllTextAsync(Path.Combine(output, "producer.json"), JsonSerializer.Serialize(new
        { model, digest, rubric = "email-copy-v1", endpoint = "v1/systemone", capturedAt = DateTimeOffset.UtcNow }, json));
    }

    /// <summary>Asks a small local model for a new authored document, then validates critical facts before human review.</summary>
    /// <remarks>This returns a proposal. It neither registers a candidate nor assigns/delivers an email.</remarks>
    public static async Task<EmailDraft> RewriteAsync(HttpClient client, string model, EmailDraft source, CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsJsonAsync("api/generate", new
        {
            model, stream = false, think = false,
            format = new
            {
                type = "object", properties = new { subject = new { type = "string" }, html = new { type = "string" } },
                required = new[] { "subject", "html" }, additionalProperties = false
            },
            prompt = "Rewrite this marketing email for clarity. You may rearrange blocks and move the main call to action. "
                + "Keep every product, price, delivery fact and link destination unchanged. Preserve the unsubscribe link. "
                + "Do not add deadlines, claims or scarcity. Return only subject and HTML. This is a proposal for human review.\n"
                + JsonSerializer.Serialize(new { source.Subject, source.Html }),
            options = new { temperature = .2, num_predict = 1800 }
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var envelope = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        using var proposal = JsonDocument.Parse(envelope.RootElement.GetProperty("response").GetString()!);
        var draft = source with { Subject = proposal.RootElement.GetProperty("subject").GetString()!,
            Html = proposal.RootElement.GetProperty("html").GetString()!, Author = "ollama:" + model, Control = false };
        ValidateRewrite(source, draft);
        return draft;
    }

    /// <summary>Checks observable link/price invariants. A human still reviews claims, meaning and execution settings.</summary>
    public static void ValidateRewrite(EmailDraft original, EmailDraft proposal)
    {
        var before = EmailCharacterisation.Analyse(original.Subject, original.Html);
        var after = EmailCharacterisation.Analyse(proposal.Subject, proposal.Html);
        if (after.PrimaryCta is null || !after.Links.Any(l => l.Unsubscribe)
            || after.Links.Any(l => before.Links.All(b => b.Destination != l.Destination)))
            throw new ArgumentException("Rewrite changed an approved destination or removed the CTA/unsubscribe link.");
        var oldPrices = Regex.Matches(System.Net.WebUtility.HtmlDecode(original.Html), @"£\s*(\d+(?:\.\d{1,2})?)")
            .Select(m => decimal.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)).ToHashSet();
        var newPrices = Regex.Matches(System.Net.WebUtility.HtmlDecode(proposal.Html), @"£\s*(\d+(?:\.\d{1,2})?)")
            .Select(m => decimal.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)).ToHashSet();
        if (!newPrices.SetEquals(oldPrices)) throw new ArgumentException("Rewrite altered visible prices; review the proposal manually.");
    }

    /// <summary>Creates a caller-owned local Ollama client; no model is downloaded by this code.</summary>
    public static HttpClient Client() => new()
    {
        BaseAddress = new Uri((Environment.GetEnvironmentVariable("OLLAMA_HOST") ?? "http://maxo:11434").TrimEnd('/') + "/"),
        Timeout = TimeSpan.FromMinutes(2)
    };
}
