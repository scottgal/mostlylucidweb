using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace Mostlylucid.Relevance.ExperimentDemo;

/// <summary>One exact visible link occurrence. Identical destinations can have different placements and IDs.</summary>
public sealed record EmailLink(string Id, string Text, string OriginalHref, string Destination, string Region,
    bool ButtonLike, bool Unsubscribe, double TextPosition);
/// <summary>Measured HTML structure. TextPosition is source text order, never an above-the-fold claim.</summary>
public sealed record EmailStructure(string Subject, string Preheader, string VisibleText, int Words, int Images,
    double? AltCoverage, IReadOnlyList<string> Headings, IReadOnlyList<EmailLink> Links, EmailLink? PrimaryCta,
    int PrimaryDestinationOccurrences);
/// <summary>Versioned readings about actual copy; producer identity distinguishes lexical measurements from model judgements.</summary>
public sealed record EmailSemantics(string Producer, string Version, double DeliveryExplanation, double OffersHelp);

/// <summary>Offline parser: no rendering, scripts, remote images, DNS or email delivery side effects.</summary>
public static partial class EmailCharacterisation
{
    /// <summary>Extracts visible source-text structure and every usable link without treating tracking pixels as readers.</summary>
    public static EmailStructure Analyse(string subject, string html, Uri? origin = null)
    {
        if (html.Length > 65536 || subject.Length > 500) throw new ArgumentException("Email exceeds the demo's bounded analysis size.");
        origin ??= new Uri("https://fieldwork.example/");
        var document = new HtmlParser().ParseDocument(html);
        var preheader = Text(document.QuerySelector("[data-preheader],.preheader")?.TextContent ?? "");
        foreach (var node in document.QuerySelectorAll("script,style,noscript,template,[hidden],[aria-hidden=true]")) node.Remove();
        foreach (var node in document.QuerySelectorAll("[style]"))
            if (HiddenStyle().IsMatch(node.GetAttribute("style") ?? "")) node.Remove();
        var body = document.Body!;
        var offsets = new Dictionary<IElement, int>();
        var visible = new StringBuilder();
        void Walk(INode node)
        {
            if (node is IElement e && e.LocalName == "a") offsets[e] = visible.Length;
            if (node is IText t)
            {
                var text = Text(t.Data);
                if (text.Length > 0) visible.Append(text).Append(' ');
            }
            else foreach (var child in node.ChildNodes) Walk(child);
        }
        Walk(body);
        var fullText = visible.ToString().Trim();
        var links = new List<EmailLink>();
        foreach (var a in body.QuerySelectorAll("a[href]"))
        {
            var href = a.GetAttribute("href") ?? "";
            if (!Uri.TryCreate(origin, href, out var target) || target.Scheme is not ("https" or "http" or "mailto")) continue;
            var label = Text(a.TextContent);
            if (label.Length == 0) label = Text(a.QuerySelector("img")?.GetAttribute("alt") ?? "");
            var region = a.Closest("footer,[role=contentinfo],[data-region=footer],.footer") is not null ? "footer"
                : a.Closest("header,nav,[role=banner],[data-region=header],.header") is not null ? "header" : "body";
            var unsubscribe = OptOut().IsMatch(label) || target.AbsolutePath.Contains("unsubscribe", StringComparison.OrdinalIgnoreCase);
            var button = a.GetAttribute("role") == "button" || ButtonClass().IsMatch(a.ClassName ?? "")
                || (a.GetAttribute("style") ?? "").Contains("background", StringComparison.OrdinalIgnoreCase);
            links.Add(new($"link-{links.Count + 1:00}", label, href, Canonical(target), region, button, unsubscribe,
                offsets.GetValueOrDefault(a) / (double)Math.Max(1, visible.Length)));
        }
        var primary = links.Where(l => !l.Unsubscribe && l.Region == "body" && l.Destination.StartsWith("http", StringComparison.Ordinal))
            .OrderByDescending(l => l.ButtonLike).ThenBy(l => l.TextPosition).FirstOrDefault();
        var images = body.QuerySelectorAll("img");
        return new(subject, preheader, fullText, Words().Matches(fullText).Count, images.Length,
            images.Length == 0 ? null : images.Count(i => !string.IsNullOrWhiteSpace(i.GetAttribute("alt"))) / (double)images.Length,
            body.QuerySelectorAll("h1,h2,h3").Select(e => Text(e.TextContent)).ToArray(), links, primary,
            primary is null ? 0 : links.Count(l => l.Destination == primary.Destination));
    }

    /// <summary>Reproducible cheap baseline: literal phrases in the source, not inferred tone or model probabilities.</summary>
    public static EmailSemantics LiteralReadings(EmailStructure structure) => new("literal-phrases", "v1",
        structure.VisibleText.Contains("delivery costs", StringComparison.OrdinalIgnoreCase) ? 1 : 0,
        structure.VisibleText.Contains("help choosing", StringComparison.OrdinalIgnoreCase) ? 1 : 0);

    /// <summary>Actual optional System One call over extracted text; strict missing/out-of-range answers fail registration.</summary>
    /// <remarks>Questions describe properties, not recipient behaviour. Never substitute these readings for open/click telemetry.</remarks>
    public static async Task<EmailSemantics> DecideAsync(HttpClient client, string model, string modelVersion,
        EmailStructure structure, CancellationToken cancellationToken = default)
    {
        object Question(string instruction, string yes, string no) => new { type = "noul", instructions = instruction,
            criteria = new Dictionary<string, string> { ["true"] = yes, ["false"] = no } };
        using var response = await client.PostAsJsonAsync("v1/systemone", new
        {
            model,
            state = new { structure.Subject, structure.Preheader, text = structure.VisibleText,
                links = structure.Links.Select(l => new { l.Text, l.Region, l.TextPosition }) },
            questions = new
            {
                delivery_explanation = Question("Does this email explain the cost or availability of delivery?",
                    "States delivery charges, free delivery, or where delivery charges can be checked.", "Does not explain delivery costs or availability."),
                offers_help = Question("Does this email offer help with choosing or comparing the product?",
                    "Explicitly offers assistance, comparison guidance, or help choosing.", "Only promotes buying without offering help choosing.")
            }
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var answers = json.RootElement.GetProperty("answers");
        double Read(string key)
        {
            var value = answers.GetProperty(key).GetProperty("noul").GetDouble();
            if (!double.IsFinite(value) || value is < 0 or > 1) throw new ArgumentException("Invalid Noul reading.");
            return value;
        }
        return new(model, modelVersion, Read("delivery_explanation"), Read("offers_help"));
    }

    private static string Canonical(Uri uri)
    {
        if (uri.Scheme == "mailto") return uri.AbsoluteUri;
        var parts = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !Uri.UnescapeDataString(p.Split('=')[0]).StartsWith("utm_", StringComparison.OrdinalIgnoreCase));
        return new UriBuilder(uri) { Query = string.Join('&', parts) }.Uri.AbsoluteUri;
    }
    private static string Text(string text) => Regex.Replace(text, @"\s+", " ").Trim();
    [GeneratedRegex(@"display\s*:\s*none|visibility\s*:\s*hidden", RegexOptions.IgnoreCase)] private static partial Regex HiddenStyle();
    [GeneratedRegex(@"\b(unsubscribe|opt out|manage preferences)\b", RegexOptions.IgnoreCase)] private static partial Regex OptOut();
    [GeneratedRegex(@"\b(button|btn|cta)\b", RegexOptions.IgnoreCase)] private static partial Regex ButtonClass();
    [GeneratedRegex(@"\b[\p{L}\p{N}]+\b")] private static partial Regex Words();
}
