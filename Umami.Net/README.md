# Umami.Net

**Version 1.1.0** · [Upgrade guide](https://github.com/scottgal/mostlylucidweb/blob/umamiv1.1.0/Umami.Net/MIGRATION.md) · [Release notes](https://github.com/scottgal/mostlylucidweb/blob/umamiv1.1.0/Umami.Net/RELEASE.md)

A .NET 8, 9 and 10 client for [Umami Analytics](https://umami.is), reviewed against
[Umami 3.4.0](https://github.com/umami-software/umami/releases/tag/v3.4.0).

The library sends page views, events, visitor identity and Web Vitals, and reads
website analytics. It supports self-hosted username/password or API-key
credentials, and the current Umami Cloud Bearer API-key protocol.

## Install and register

```sh
dotnet add package Umami.Net --version 1.1.0
```

```csharp
builder.Services.SetupUmamiClient(builder.Configuration); // Collection and background sender
builder.Services.SetupUmamiData(builder.Configuration);   // Authenticated analytics API
```

Both extensions use the `Analytics` configuration section. Register either or
both. Registration does not change your application's logging level or providers.

```json
{
  "Analytics": {
    "UmamiPath": "https://analytics.example.com/",
    "WebsiteId": "12345678-1234-1234-1234-123456789abc",
    "ApiVersion": "V3"
  }
}
```

Set secrets through user secrets or environment variables rather than committing
them. For self-hosted Umami 3.4, set `Analytics__ApiKey` to an account API key
created under Settings → API keys. The client sends `Authorization: Bearer ...`
and does not call the auth endpoints, which are blocked for these keys.

Alternatively, supply `Analytics__Username` and `Analytics__Password`. The client
logs in, verifies existing tokens with POST `/api/auth/verify`, and retries a
401 once after forcing a fresh login. Website permission failures then return
401 without recurring indefinitely. A 2FA partial token is not accepted as a
completed login; use an API key for unattended access to a 2FA-enabled account.

For Umami Cloud, configure `UmamiPath` as `https://api.umami.is/v1/` (or a
region endpoint such as `/v1/eu/`), `UseCloudApi: true`, and `ApiKey`. The
client preserves that base path and omits the self-hosted `/api` prefix.
Collection uses the tracker/collection host, not the authenticated Cloud data
API; register the collection client separately if your endpoints differ.

API keys and the modern analytics routes require a server that supports them.
This package does not upgrade your Umami server, migrate its database or enable
server features such as session replay or MCP.

## Tracking

Inject `UmamiClient` for immediate collection:

```csharp
using var response = await client.TrackPageView("/products?utm_source=newsletter", "Products");
response.EnsureSuccessStatusCode();

using var purchase = await client.Track("purchase", new UmamiEventData
{
    ["revenue"] = 49.95,
    ["currency"] = "GBP"
}, distinctId: "customer-123");
```

Methods returning `HttpResponseMessage` transfer ownership to the caller; dispose
the response. `*AndDecode` methods dispose their response and return analytics
metadata instead. `SendAsync` adds cancellation support without changing the
existing `Send`, `Track`, `TrackPageView` or `Identify` signatures.

The current collector returns `{ cache, sessionId, visitId }`. The decoded result
exposes `SessionId`, `VisitId` and `Cache`; `Id` also contains the current session
ID. Legacy bare-JWT responses are still decoded as analytics metadata. They are
not used as proof of authentication. `{ "beep": "boop" }` returns `BotDetected`.

```csharp
var result = await client.TrackPageViewAndDecode("/landing");
using var next = await client.SendAsync(new UmamiPayload
{
    Url = "/pricing",
    CacheToken = result?.Cache,
    DistinctId = "customer-123",
    Tag = "campaign-a",
    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
}, cancellationToken: cancellationToken);
```

Cache tokens belong to a specific visitor. They are passed explicitly on the
payload as `x-umami-cache`; the client never stores one in shared default headers.
Umami 3.4 identifies visitors using `DistinctId` (`id` on the wire). `SessionId`
on the payload is retained for legacy clients; current servers calculate session
IDs rather than accepting a caller-selected session UUID.

Explicit event data, IP address and user-agent overrides survive payload
population. Without overrides, the client captures the inbound user agent, path
base, path and query string. Query strings retain UTM parameters. In a reverse
proxy deployment, configure ASP.NET Core's trusted forwarded-header handling
before collecting events so `RemoteIpAddress` represents the intended visitor.

`UseDefaultUserAgent` is an explicit override retained for existing integrations;
it stores the original agent in event data. Normal collection preserves the real
visitor agent and allows Umami's bot filtering to apply.

### Web Vitals

```csharp
using var response = await client.TrackPerformanceAsync(new UmamiPayload
{
    Url = "/checkout", Title = "Checkout",
    Lcp = 1800, Inp = 120, Cls = 0.04, Fcp = 950, Ttfb = 180
});
```

Timing fields are milliseconds; CLS is a unitless score. Values are checked
against the current collector's ranges. `Timestamp` is Unix **seconds**;
analytics request dates are converted to Unix **milliseconds**.

### Background delivery

Inject `UmamiBackgroundSender` to capture request metadata and enqueue delivery:

```csharp
await sender.TrackPageView("/products", "Products");
await sender.Track("checkout", new UmamiEventData { ["cartSize"] = 3 });
await sender.Identify(distinctId: "customer-123");
```

The in-memory queue holds up to 1,024 events. Enqueue operations wait when full,
providing backpressure instead of unbounded memory growth. Shutdown completes
the writer and drains pending events until the host cancellation deadline, then
cancels delivery. Process crashes can lose queued data; use a durable outbox if
that is unacceptable. Failed delivery is logged without stopping the reader.
Tracking writes are not retried automatically, because a lost acknowledgement
can otherwise double-count an event.

## Existing analytics methods

Inject `UmamiDataService`:

```csharp
var request = new MetricsRequest
{
    StartAtDate = DateTime.UtcNow.AddDays(-7),
    EndAtDate = DateTime.UtcNow,
    Type = MetricType.path,
    Limit = 20,
    UtmSource = "newsletter"
};
var metrics = await data.GetMetrics(request);
if (metrics.Status == HttpStatusCode.OK)
    foreach (var row in metrics.Data ?? []) Console.WriteLine($"{row.x}: {row.y}");
```

Existing methods remain available: `GetStats`, `GetPageViews`, `GetMetrics`,
`GetExpandedMetrics`, `GetEventsSeries`, and `GetActiveUsers`. Stats normalize
both legacy `{ value, prev }` fields and current numeric totals plus `comparison`
into the existing `.pageviews.value` / `.pageviews.prev` model. Active visitors
accept legacy `x` and current `visitors` fields.

`ApiVersion` defaults to `V3`. `MetricType.url` maps to `type=path` in V3. Set
`V2` to retain the old metric value; set `V1` for the explicit legacy
`path` → `url` and `hostname` → `host` parameter mapping. Version detection is
not automatic. Invalid 400 requests are not silently rewritten and resubmitted.
Compatibility mappings do not promise every feature exists on older servers.

Metric dimensions include `distinctId` and `utmSource`, `utmMedium`,
`utmCampaign`, `utmContent`, `utmTerm`; their camelCase wire names are preserved.
`MetricType.event` lists event names without requiring an event filter.
Minute buckets are supported; event series default to UTC when no timezone is supplied.
Common requests include distinct-identity, UTM, event-type, match and bounce
filters. Filters are URL-encoded once; operators and regex commas are preserved.

## Current website analytics

Umami 3.4 exposes website-scoped GET endpoints for report calculations:

```csharp
var report = await data.GetAnalyticsAsync(AnalyticsEndpoint.PerformanceChart,
    new AnalyticsRequest
    {
        StartAtDate = DateTime.UtcNow.AddDays(-7), EndAtDate = DateTime.UtcNow,
        Unit = Unit.day, Timezone = "Europe/London", Metric = "lcp"
    }, cancellationToken);
```

`AnalyticsEndpoint` covers attribution, breakdown, funnel/goal stats, heatmaps,
journeys, retention, performance stats/chart/metrics, UTM metrics, revenue
stats/chart/metrics, sessions and events. Criteria vary by endpoint; use the
[official contracts](https://github.com/umami-software/umami/blob/v3.4.0/docs/report-api-migration.md).
For example, revenue requires `Currency`, UTM requires a `Type` such as
`utm_source`, and a funnel preview takes JSON `Steps` plus a `Window`.

The default result is `UmamiResult<JsonElement>`, preserving each dataset's
actual shape. `GetAnalyticsAsync<T>` supports caller-defined typed models.
These methods expose analytics datasets; they do not render the Umami UI or
record browser session replay streams.

### Annotations

```csharp
var note = await data.CreateAnnotationAsync(new AnnotationInput(
    DateTimeOffset.UtcNow, "Website refresh deployed", AllDay: false), cancellationToken);
var notes = await data.GetAnnotationsAsync(page: 1, pageSize: 20, cancellationToken: cancellationToken);
// UpdateAnnotationAsync(id, input), GetAnnotationAsync(id), DeleteAnnotationAsync(id)
```

Annotations have typed inputs, responses and paginated lists. Creating/updating
uses POST, and deletion uses DELETE, matching the current contracts. Notes must
contain 1–500 characters. These methods execute writes only when explicitly called.

### Other website resources

`SendWebsiteRequestAsync<T>` handles additional documented resources, including
saved funnels/goals, segments, session detail and replay metadata. Supply a
relative website resource path; it rejects absolute URLs, traversal and embedded
query strings. The website ID and host always come from configuration.

```csharp
var goals = await data.SendWebsiteRequestAsync<JsonElement>(HttpMethod.Get, "goals",
    new Dictionary<string, string?> { ["page"] = "1", ["pageSize"] = "20" },
    cancellationToken: cancellationToken);
```

The method preserves structured JSON and allows typed result models. Resource
schemas and permissions remain those of the configured Umami server. It does
not expose account API-key management or administrative routes.

For assistant access, Umami 3.4 provides its own [MCP server](https://github.com/umami-software/umami/blob/v3.4.0/packages/mcp/README.md).
Use that server with an appropriate API key; this package is an HTTP analytics
client, not an MCP implementation.

## Failures and cancellation

Analytics transport failures return `UmamiResult<T>`: 503 for a network failure,
504 for an HTTP timeout, 502 for an invalid JSON response, and the server status
for HTTP failures. Explicit caller cancellation propagates as
`OperationCanceledException`. Invalid local arguments and date ranges throw
before sending requests. Responses and failed retry attempts are disposed.

Only GET requests receive bounded transient/429 retries in the DI registration.
401 reauthentication is separately bounded to one retry. Mutating requests are
not retried for transient transport errors. API-key 401 responses do not trigger
a password fallback. Logs omit auth/cache tokens, payloads and query strings.

## Validation and release

```sh
dotnet test Umami.Net.Test/Umami.Net.Test.csproj
scripts/test-umami-integration.sh # Spins up disposable Umami 3.4.0 + PostgreSQL; 51 real cases per target
dotnet build Umami.Net/Umami.Net.csproj -c Release
dotnet pack Umami.Net/Umami.Net.csproj -c Release --no-build -o /tmp/umami-pack
```

The package targets net8.0, net9.0 and net10.0. Release tags use `umamiv*.*.*`;
website `release-*` tags do not publish NuGet packages. See
[review findings and verification](https://github.com/scottgal/mostlylucidweb/blob/main/Umami.Net/REVIEW.md) for the scope of the current update.
