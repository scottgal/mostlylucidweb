using System.Net;
using System.Text;
using Mostlylucid.Relevance.ExperimentDemo;

namespace Mostlylucid.Relevance.Tests;

public sealed class EmailCharacterisationTests
{
    [Fact]
    public void ExtractsLinkOccurrencesHiddenTextAndLogicalDestinationsWithoutInventingRenderedPositions()
    {
        var html = "<body><div style='display:none'><a href='/hidden'>Hidden</a></div><header><a href='/'>Home</a></header>"
            + "<p>A product summary.</p><a class='cta' href='/product?id=7&utm_source=email'>Review</a>"
            + "<p>More detail.</p><a href='/product?id=7&utm_medium=reminder'>Again</a><a href='/product?id=8'>Other</a>"
            + "<img src='https://never-fetched.invalid/pixel' alt='Boots'><footer><a href='/unsubscribe'>Unsubscribe</a></footer></body>";
        var s = EmailCharacterisation.Analyse("A subject", html);
        Assert.DoesNotContain("Hidden", s.VisibleText);
        Assert.Equal(5, s.Links.Count); Assert.Equal("link-02", s.PrimaryCta!.Id);
        Assert.Equal(2, s.PrimaryDestinationOccurrences);
        Assert.Contains("id=7", s.PrimaryCta.Destination); Assert.DoesNotContain("utm_", s.PrimaryCta.Destination);
        Assert.NotEqual(s.Links[1].Id, s.Links[2].Id);
        Assert.NotEqual(s.Links[1].TextPosition, s.Links[2].TextPosition);
        Assert.Equal("header", s.Links[0].Region); Assert.Equal("footer", s.Links[^1].Region);
        Assert.True(s.Links[^1].Unsubscribe); Assert.Equal(1, s.AltCoverage);
    }

    [Fact]
    public void ActualStructuralEditsChangeCoordinatesAndContentIdentity()
    {
        var drafts = EmailVariants.Generate(); Assert.Equal(48, drafts.Count); Assert.Single(drafts, d => d.Control);
        var late = drafts.First(d => d.Control);
        var early = late with { Html = "<body><a class='cta' href='https://fieldwork.example/products/walking-boots'>View boots</a><p>" + new string('x', 200) + "</p></body>", Control = false };
        var a = EmailVariants.Characterise(late); var b = EmailVariants.Characterise(early);
        Assert.NotEqual(a.Variant.Id, b.Variant.Id);
        Assert.True(a.Structure.PrimaryCta!.TextPosition > b.Structure.PrimaryCta!.TextPosition);
        Assert.True(a.Variant.Features[1] > b.Variant.Features[1]);
        Assert.Null(b.Structure.AltCoverage);
    }

    [Fact]
    public void MissingCtaCannotBecomeAZeroCoordinateAndUnsubscribeIsNotThePrimaryAction()
    {
        var draft = EmailVariants.Generate()[0] with { Html = "<p>Hello</p><footer><a href='/unsubscribe'>Unsubscribe</a></footer>" };
        Assert.Null(EmailCharacterisation.Analyse(draft.Subject, draft.Html).PrimaryCta);
        Assert.Throws<ArgumentException>(() => EmailVariants.Characterise(draft));
    }

    [Fact]
    public async Task DecisionModelReadsActualCopyAndRejectsMissingReadings()
    {
        var handler = new ResponseHandler("""{"answers":{"delivery_explanation":{"noul":0.93},"offers_help":{"noul":0.12}}}""");
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://model.invalid/") };
        var source = EmailCharacterisation.Analyse("Boots", "<p>Shipping is included. Ask us about fit.</p>");
        var reading = await EmailCharacterisation.DecideAsync(client, "test-model", "digest-1", source);
        Assert.Equal(.93, reading.DeliveryExplanation); Assert.Equal(.12, reading.OffersHelp);
        Assert.Contains("Shipping is included", handler.Body);
        Assert.Contains("v1/systemone", handler.Url);
        handler.Json = """{"answers":{"delivery_explanation":{"noul":0.93}}}""";
        await Assert.ThrowsAsync<KeyNotFoundException>(() => EmailCharacterisation.DecideAsync(client, "test-model", "digest-1", source));
    }

    [Fact]
    public void RewriteMustPreserveVisiblePricesAndApprovedDestinations()
    {
        var before = EmailVariants.Generate()[0];
        EmailModels.ValidateRewrite(before, before with { Subject = "Your shortlist, together" });
        Assert.Throws<ArgumentException>(() => EmailModels.ValidateRewrite(before, before with { Html = before.Html.Replace("£100.00", "£85.00") }));
        Assert.Throws<ArgumentException>(() => EmailModels.ValidateRewrite(before, before with { Html = before.Html.Replace("https://fieldwork.example/", "https://other.invalid/") }));
    }

    private sealed class ResponseHandler(string json) : HttpMessageHandler
    {
        public string Json { get; set; } = json;
        public string Body { get; private set; } = "";
        public string Url { get; private set; } = "";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = await request.Content!.ReadAsStringAsync(cancellationToken); Url = request.RequestUri!.ToString();
            return new(HttpStatusCode.OK) { Content = new StringContent(Json, Encoding.UTF8, "application/json") };
        }
    }
}
