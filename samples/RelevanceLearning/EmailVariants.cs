using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mostlylucid.Relevance.Learning;

namespace Mostlylucid.Relevance.ExperimentDemo;

/// <summary>Actual authored/generated content and execution settings before vectorisation.</summary>
public sealed record EmailDraft(string Subject, string Html, decimal OriginalPrice, decimal OfferPrice,
    int DelayMinutes, string Author, bool Control = false);
/// <summary>Inspectable source, extracted structure, producer readings and the registered candidate.</summary>
public sealed record CharacterisedEmail(EmailDraft Draft, EmailStructure Structure, EmailSemantics Semantics,
    InterventionVariant Variant);

/// <summary>Shared configuration: the existing Relevance engine ranks the marketing emails themselves.</summary>
public static class EmailVariants
{
    /// <summary>Configures independent structural, copy, timing and offer contributors, with positive observed-response summaries.</summary>
    public static RelevanceDefinition RelevanceDefinition(string version = "literal-v1") => global::Mostlylucid.Relevance.RelevanceDefinition.FromYaml($$"""
        id: email-candidates-{{version}}
        candidate_limit: 64
        max_candidates: 64
        points:
          structure: {path: structure, converter: vector, dimensions: 5, metric: l2}
          meaning: {path: meaning, converter: vector, dimensions: 2, metric: l2}
          timing: {path: timing, converter: vector, dimensions: 1, metric: l2}
          offer: {path: offer, converter: vector, dimensions: 1, metric: l2}
        streams:
          emails: {kind: candidate}
          responses:
            kind: observation
            weights: {helpful: 1}
        placements:
          campaign:
            heads: {structure: 1, meaning: 1, timing: 1, offer: 1}
        """);

    /// <summary>Four continuous intervention properties plus repeat-link structure; versions isolate producer/objective semantics.</summary>
    public static LearningDefinition LearningDefinition(string version = "literal-v1") => new()
    {
        Id = "email-effects-" + version, Version = "email-parser-1;" + version + ";margin-1",
        ContextAxes = ["comparisons_per_view", "price_narrowing"],
        InterventionAxes = ["discount_over_10_percent", "primary_cta_text_position", "delivery_explanation",
            "delay_over_60_minutes", "primary_destination_occurrences_over_4"],
        MaxVariants = 64, MinOutcomes = 100, Exploration = .25, HorizonSeconds = 3600
    };

    /// <summary>Builds a small factorial set: prices, link placement, delivery copy, delay and repeated destination links.</summary>
    /// <remarks>These are generated documents to parse, not pre-labelled feature vectors. No email is actually sent.</remarks>
    public static IReadOnlyList<EmailDraft> Generate(decimal originalPrice = 100m)
    {
        var drafts = new List<EmailDraft>();
        foreach (var discount in new[] { 0m, .05m, .10m })
        foreach (var early in new[] { false, true })
        foreach (var delivery in new[] { false, true })
        foreach (var delay in new[] { 10, 40 })
        foreach (var repeat in new[] { false, true })
        {
            var price = decimal.Round(originalPrice * (1 - discount), 2);
            var cta = $"<p><a class=\"cta\" href=\"https://fieldwork.example/products/walking-boots?utm_source=email\">View the walking boots for £{price:0.00}</a></p>";
            var deliveryCopy = delivery ? "Delivery costs are shown before payment, and standard delivery is free over £75."
                : "The full product page has the current sizes and materials for this pair of boots.";
            var content = "<p>You compared walking boots during your visit. Here is the pair you looked at, with the details together.</p>";
            var benefits = string.Join("", new[]
            {
                "Take another look at the fit, the materials and the available sizes before deciding whether these are right for your walks.",
                "Compare the product details with your shortlist. You can return to the same selection without starting the search again.",
                "Your basket is a place to keep possibilities together. There is no need to complete a purchase to review the information."
            }.Select(text => "<p>" + text + "</p>"));
            var html = "<!doctype html><html><body><div class=\"preheader\" style=\"display:none\">Your walking shortlist, together.</div>"
                + "<header><a href=\"https://fieldwork.example/\">Fieldwork</a></header><main><h1>Your walking shortlist</h1>" + content
                + (early ? cta : "") + benefits + "<p>" + deliveryCopy + "</p>" + (early ? "" : cta)
                + (repeat ? "<p><a href=\"https://fieldwork.example/products/walking-boots?utm_medium=reminder\">See these boots again</a></p>" : "")
                + "<p>Need help choosing? Reply to ask about your shortlist.</p></main>"
                + "<footer><a href=\"https://fieldwork.example/unsubscribe\">Unsubscribe</a></footer></body></html>";
            drafts.Add(new("Your walking shortlist", html, originalPrice, price, delay, "template-generator-v1",
                discount == 0 && !early && !delivery && delay == 40 && !repeat));
        }
        return drafts;
    }

    /// <summary>Builds vectors from the actual parsed content plus trusted numeric settings, retaining the exact source and provenance.</summary>
    /// <param name="draft">A template, human edit or model rewrite. Its identity is content/settings derived.</param>
    /// <param name="semantics">Optional actual model readings; absent selects the explicitly labelled lexical baseline.</param>
    public static CharacterisedEmail Characterise(EmailDraft draft, EmailSemantics? semantics = null)
    {
        if (draft.OriginalPrice <= 0 || draft.OfferPrice < draft.OriginalPrice * .9m || draft.OfferPrice > draft.OriginalPrice
            || draft.DelayMinutes is < 0 or > 60) throw new ArgumentException("Offer or delay is outside this experiment's approved bounds.");
        var structure = EmailCharacterisation.Analyse(draft.Subject, draft.Html);
        var cta = structure.PrimaryCta ?? throw new ArgumentException("This email family requires an identifiable body CTA.");
        semantics ??= EmailCharacterisation.LiteralReadings(structure);
        var readings = new[] { semantics.DeliveryExplanation, semantics.OffersHelp };
        if (readings.Any(v => !double.IsFinite(v) || v is < 0 or > 1)) throw new ArgumentException("Missing or invalid semantic readings.");
        var discount = (double)((draft.OriginalPrice - draft.OfferPrice) / draft.OriginalPrice / .1m);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { draft, semantics }))));
        var id = "mail-" + hash[..16].ToLowerInvariant();
        var structural = new[] { cta.TextPosition, Math.Min(1, structure.Links.Count / 10.0), Math.Min(1, structure.Words / 500.0),
            Math.Min(1, structure.Images / 5.0), Math.Min(1, structure.PrimaryDestinationOccurrences / 4.0) };
        var descriptor = new[] { discount, cta.TextPosition, semantics.DeliveryExplanation, draft.DelayMinutes / 60.0, structural[4] };
        var candidate = JsonSerializer.SerializeToElement(new
        {
            id, draft.Subject, draft.Html, draft.OriginalPrice, draft.OfferPrice, draft.DelayMinutes, draft.Author,
            content_hash = hash, semantics.Producer, semantics.Version, structure = structural,
            meaning = readings, timing = new[] { draft.DelayMinutes / 60.0 }, offer = new[] { discount }
        });
        return new(draft, structure, semantics, new InterventionVariant(id, descriptor, candidate, draft.Control));
    }

    /// <summary>Injects editable human text into a bounded HTML document, safely encoding plain text for the demo editor.</summary>
    public static string PlainCopyHtml(string heading, string body, string linkLabel) => "<html><body><h1>"
        + WebUtility.HtmlEncode(heading) + "</h1><p>" + WebUtility.HtmlEncode(body) + "</p><p><a class=\"cta\" href=\"https://fieldwork.example/products/walking-boots\">"
        + WebUtility.HtmlEncode(linkLabel) + "</a></p><footer><a href=\"https://fieldwork.example/unsubscribe\">Unsubscribe</a></footer></body></html>";
}
