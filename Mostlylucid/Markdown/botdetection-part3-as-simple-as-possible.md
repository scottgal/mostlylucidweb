# **StyloBot: As Simple As Possible And No Simpler (Part 3)**

*Enterprise bot detection shouldn't require a PhD in infrastructure (or thousands a month in expenditure to use!). Two lines of code, zero external services, and you're running (up to) 21 detectors in milliseconds on every request to every endpoint.*

**[Read Part 1: StyloBot: Fighting Back Against Scrapers](https://www.mostlylucid.net/blog/botdetection-introduction)**

**[Read Part 2: How Bots Got Smarter](https://www.mostlylucid.net/blog/botdetection-part2-signature-pipeline-and-stylobot-architecture)**

**[👉 See It Live: StyloBot.net](https://stylobot.net)** - The real production system running early-exit detection inline at the gateway.

<!--category-- ASP.NET, Bot Detection, Security, Architecture -->
<datetime class="hidden">2026-02-16T10:30</datetime>

[![NuGet](https://img.shields.io/nuget/v/mostlylucid.botdetection.svg)](https://www.nuget.org/packages/mostlylucid.botdetection/)
[![GitHub](https://img.shields.io/github/stars/scottgal/stylobot?style=social)](https://github.com/scottgal/stylobot)
[![Docker](https://img.shields.io/docker/pulls/scottgal/stylobot-gateway)](https://hub.docker.com/r/scottgal/stylobot-gateway)

---

[TOC]

---

## The Idea

Einstein supposedly said, "Everything should be made as simple as possible, but no simpler." That's the design principle behind StyloBot's integration model.

Parts 1 and 2 covered *why* bot detection matters and *how the detection pipeline works*. This post covers *how little code you actually need* - and how the same system scales from a single-file app to a full production gateway with TimescaleDB, Qdrant vector search, and CPU-only LLM classification.

The key insight: **every tier uses the same detection pipeline**. You're not switching frameworks as you grow. You're adding storage and enrichment around the same core.

---

## Two Lines of Code

This is the absolute minimum. No config file, no database setup, no API keys, no Docker containers.

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBotDetection();       // ← that's line 1

var app = builder.Build();
app.UseBotDetection();                    // ← that's line 2
app.Run();
```

What just happened:
- **21 detectors** registered: UserAgent pattern matching, header consistency, IP datacenter detection, behavioral analysis, TLS fingerprinting (JA3/JA4), TCP/IP fingerprinting, HTTP/2 fingerprinting, cache behavior analysis, response behavior feedback, multi-layer correlation, and more.
- **Wave-based pipeline**: Detectors run in dependency waves. Wave 0 (no dependencies) executes in parallel. Later waves trigger only when earlier signals warrant deeper analysis.
- **SQLite storage**: A `botdetection.db` file auto-creates for learned patterns and weights. No setup.
- **In-process similarity search**: HNSW index for finding similar bot signatures. No external vector database.
- **Heuristic scoring**: ~50 features extracted per request, scored by a lightweight in-process model.

All of this runs in **under 1 millisecond** per request on commodity hardware. CPU only, no GPU.

Every request now has detection results available via `HttpContext` extensions:

```csharp
app.MapGet("/", (HttpContext ctx) => Results.Ok(new
{
    isBot = ctx.IsBot(),
    probability = ctx.GetBotProbability(),     // 0.0-1.0: how likely it's a bot
    confidence = ctx.GetDetectionConfidence(),  // 0.0-1.0: how certain the system is
    type = ctx.GetBotType()?.ToString(),
    name = ctx.GetBotName()
}));
```

Detection runs but nothing blocks. You decide what to do with the results.

---

## Block All Bots, Whole App

If you just want to block bots from your entire application - no per-endpoint config, no attributes - it's one line of JSON:

```json
{
  "BotDetection": {
    "BlockDetectedBots": true
  }
}
```

That's it. Detected bots above your block-confidence threshold get a 403 (`MinConfidenceToBlock` defaults to `0.8`). Search engines (Googlebot, Bingbot), social media previews (Facebook, Twitter/X), and monitoring bots (UptimeRobot, Pingdom) are allowed through by default - because you almost certainly want those.

Or the same thing in code, no config file needed:

```csharp
builder.Services.Configure<BotDetectionOptions>(o =>
{
    o.BlockDetectedBots = true;
    o.MinConfidenceToBlock = 0.8;           // only block when confident
    o.AllowVerifiedSearchEngines = true;     // Googlebot, Bingbot through
    o.AllowSocialMediaBots = true;           // Facebook, Twitter previews through
    o.AllowMonitoringBots = true;            // UptimeRobot, Pingdom through
});
```

This is the "I don't want to think about it" mode. Detection runs, bots get blocked, good crawlers get through. Move to per-endpoint control when you need it.

---

## Minimal API: The Complete Example

Here's a complete, working API with per-endpoint bot protection. This is the entire `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBotDetection();

var app = builder.Build();
app.UseBotDetection();

// Detection results available, no blocking
app.MapGet("/", (HttpContext ctx) => Results.Ok(new
{
    isBot = ctx.IsBot(),
    probability = ctx.GetBotProbability(),
    confidence = ctx.GetDetectionConfidence(),
    type = ctx.GetBotType()?.ToString(),
    name = ctx.GetBotName()
}));

// Block all bots
app.MapGet("/api/data", () => Results.Ok(new { data = "sensitive" }))
   .BlockBots();

// Allow search engines (Googlebot, Bingbot, Yandex)
app.MapGet("/products", () => Results.Ok(new { catalog = "public" }))
   .BlockBots(allowSearchEngines: true);

// Allow search engines + social media previews (Facebook, Twitter/X)
app.MapGet("/blog/{slug}", (string slug) => Results.Ok(new { post = slug }))
   .BlockBots(allowSearchEngines: true, allowSocialMediaBots: true);

// Health check: monitoring bots allowed (UptimeRobot, Pingdom)
app.MapGet("/health", () => Results.Ok("healthy"))
   .BlockBots(allowMonitoringBots: true);

// Humans only - blocks ALL bots including verified crawlers
app.MapPost("/api/submit", () => Results.Ok(new { submitted = true }))
   .RequireHuman();

// High-confidence blocking only (reduces false positives)
app.MapGet("/api/lenient", () => Results.Ok("data"))
   .BlockBots(minConfidence: 0.9);

// Geo + network blocking (needs GeoDetection contributor)
app.MapPost("/api/payment", () => Results.Ok("ok"))
   .BlockBots(blockCountries: "CN,RU", blockVpn: true, blockDatacenter: true);

// Honeypot: deliberately allow scrapers in
app.MapGet("/honeypot", () => Results.Ok("welcome"))
   .BlockBots(allowScrapers: true, allowMaliciousBots: true);

// Dev diagnostics
app.MapBotDetectionEndpoints();

app.Run();
```

Every `.BlockBots()` call blocks **all** bot types by default. You opt specific types *in* with the `Allow*` parameters. The idea is deny-by-default, whitelist the good ones.

### Bot Types You Can Allow

| Parameter | What It Allows | Why You'd Use It |
|-----------|---------------|-----------------|
| `allowSearchEngines` | Googlebot, Bingbot, Yandex | SEO - you want to be indexed |
| `allowSocialMediaBots` | Facebook, Twitter/X, LinkedIn | Link previews, Open Graph cards |
| `allowMonitoringBots` | UptimeRobot, Pingdom, StatusCake | Health checks, uptime monitoring |
| `allowAiBots` | GPTBot, ClaudeBot, Google-Extended | Opt-in to AI training |
| `allowGoodBots` | Feed readers, link checkers | Benign automation |
| `allowVerifiedBots` | DNS-verified crawlers | Trusted automation |
| `allowScrapers` | AhrefsBot, SemrushBot | Honeypots, SEO research |
| `allowMaliciousBots` | Known bad actors | Honeypots, security research |
| `minConfidence` | *(threshold)* | Only block when system is highly certain |

---

## MVC Controllers: Attributes

Same detection pipeline, protection via attributes.

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBotDetection();
builder.Services.AddControllersWithViews();

var app = builder.Build();
app.UseBotDetection();
app.MapControllers();
app.Run();
```

### The Attributes

```csharp
[ApiController]
[Route("[controller]")]
public class ProductsController : ControllerBase
{
    // No protection - detection runs but nothing blocks
    [HttpGet]
    public IActionResult List() => Ok(new { products = "all" });

    // Block all bots, allow search engines
    [HttpGet("catalog")]
    [BlockBots(AllowSearchEngines = true)]
    public IActionResult Catalog() => Ok(new { catalog = "indexed" });

    // Block all bots, allow search engines + social previews
    [HttpGet("{id:int}")]
    [BlockBots(AllowSearchEngines = true, AllowSocialMediaBots = true)]
    public IActionResult Detail(int id) => Ok(new { id });
}

// Entire controller: humans only
[ApiController]
[Route("[controller]")]
[RequireHuman]
public class CheckoutController : ControllerBase
{
    [HttpPost("cart")]
    public IActionResult AddToCart() => Ok();

    [HttpPost("pay")]
    public IActionResult Pay() => Ok();
}

// Infrastructure endpoints
[ApiController]
[Route("[controller]")]
public class InfraController : ControllerBase
{
    // Skip detection entirely
    [HttpGet("health")]
    [SkipBotDetection]
    public IActionResult Health() => Ok("ok");

    // Monitoring bots allowed
    [HttpGet("status")]
    [BlockBots(AllowMonitoringBots = true)]
    public IActionResult Status() => Ok(new { uptime = "99.9%" });
}
```

### Geographic & Network Blocking

These work on both MVC attributes and Minimal API filters. They require the GeoDetection contributor for signal data.

```csharp
// Block countries
[BlockBots(BlockCountries = "CN,RU,KP")]
public IActionResult SensitiveApi() => Ok();

// Country whitelist - only these allowed
[BlockBots(AllowCountries = "US,GB,DE,FR")]
public IActionResult DomesticOnly() => Ok();

// Block VPNs + proxies (anti-fraud)
[BlockBots(BlockVpn = true, BlockProxy = true)]
public IActionResult Payment() => Ok();

// Block datacenter IPs + Tor
[BlockBots(BlockDatacenter = true, BlockTor = true)]
public IActionResult FormSubmission() => Ok();

// Combine: SEO-friendly + geo block + VPN block
[BlockBots(AllowSearchEngines = true, BlockCountries = "CN,RU", BlockVpn = true)]
public IActionResult ProtectedContent() => Ok();
```

---

## Beyond Block/Allow: Action Policies

Binary block/allow is simple but limited. Action policies separate *what you detect* from *how you respond*. Define response strategies in config, assign them to endpoints.

### appsettings.json

```json
{
  "BotDetection": {
    "BotThreshold": 0.7,
    "ActionPolicies": {
      "api-block": {
        "Type": "Block",
        "StatusCode": 403,
        "Message": "Bot traffic is not allowed."
      },
      "api-throttle": {
        "Type": "Throttle",
        "BaseDelayMs": 500,
        "MaxDelayMs": 5000,
        "ScaleByRisk": true,
        "JitterPercent": 0.3
      },
      "shadow-mode": {
        "Type": "LogOnly",
        "AddResponseHeaders": true,
        "LogFullEvidence": true
      }
    }
  }
}
```

### Assign Policies to Endpoints

```csharp
// Bots get progressively slower responses (they don't know they're being throttled)
[BotPolicy("default", ActionPolicy = "api-throttle")]
public IActionResult Browse() => Ok();

// Hard block
[BotPolicy("default", ActionPolicy = "api-block")]
public IActionResult Confirm() => Ok();

// Shadow mode: log everything, block nothing (deploy first, tune later)
[BotPolicy("default", ActionPolicy = "shadow-mode")]
public IActionResult PublicApi() => Ok();
```

Five policy types: `Block` (HTTP 403), `Throttle` (stealth delays), `Challenge` (CAPTCHA/proof-of-work), `Redirect` (honeypot trap), `LogOnly` (shadow mode). See the [action policies docs](https://github.com/scottgal/LLMApi/blob/main/Mostlylucid.BotDetection/docs/action-policies.md) for the full reference.

Shadow mode is the recommended starting point. Deploy detection, watch the results, tune thresholds, *then* start blocking.

---

## What You Get for Free

Every request after `UseBotDetection()` has these extensions available on `HttpContext`:

```csharp
// Am I talking to a bot?
context.IsBot()                    // true if probability >= threshold
context.IsHuman()                  // inverse
context.IsSearchEngineBot()        // Googlebot, Bingbot, etc.
context.IsVerifiedBot()            // DNS-verified bots
context.IsMaliciousBot()           // known bad actors

// How bad is it?
context.GetBotProbability()        // 0.0-1.0: likelihood of being a bot
context.GetDetectionConfidence()   // 0.0-1.0: how certain the system is
context.GetRiskBand()              // Low, Elevated, Medium, High
context.GetRecommendedAction()     // Allow, Challenge, Throttle, Block

// What is it?
context.GetBotType()               // BotType enum
context.GetBotName()               // "Googlebot", "Scrapy", etc.

// Full breakdown
var result = context.GetBotDetectionResult();
```

Two independent scores matter here: **bot probability** (how likely is this a bot?) and **detection confidence** (how certain is the system?). You can be 95% confident something is *human* (low probability, high confidence). Or you can see a suspicious request but have low confidence because only one detector ran.

---

## Signal-Based Filtering

Beyond bot types, StyloBot exposes 100+ typed signals from its detectors. You can filter endpoints based on specific signal values - for both Minimal API and MVC.

### Minimal API

```csharp
// Block VPN traffic
app.MapPost("/api/payment", () => Results.Ok())
   .BlockIfSignal(SignalKeys.GeoIsVpn, SignalOperator.Equals, "True");

// Block datacenter IPs
app.MapPost("/api/submit", () => Results.Ok())
   .BlockIfSignal(SignalKeys.IpIsDatacenter, SignalOperator.Equals, "True");

// Only allow US traffic
app.MapGet("/api/domestic", () => Results.Ok())
   .RequireSignal(SignalKeys.GeoCountryCode, SignalOperator.Equals, "US");

// Block high-confidence bots by heuristic score
app.MapGet("/api/premium", () => Results.Ok())
   .BlockIfSignal(SignalKeys.HeuristicConfidence, SignalOperator.GreaterThan, "0.9");
```

### MVC

```csharp
[BlockIfSignal(SignalKeys.GeoIsVpn, SignalOperator.Equals, "True")]
public IActionResult Payment() => Ok();

[RequireSignal(SignalKeys.GeoCountryCode, SignalOperator.Equals, "US")]
public IActionResult DomesticOnly() => Ok();
```

### Reading Signals Inline

```csharp
app.MapGet("/debug", (HttpContext ctx) =>
{
    var country = ctx.GetSignal<string>(SignalKeys.GeoCountryCode);
    var isVpn = ctx.GetSignal<bool>(SignalKeys.GeoIsVpn);
    var isDc = ctx.IsDatacenter();
    var heuristic = ctx.GetSignal<double>(SignalKeys.HeuristicConfidence);

    return Results.Ok(new { country, isVpn, isDc, heuristic });
});
```

Full signal reference: [signals and custom filters](https://github.com/scottgal/LLMApi/blob/main/Mostlylucid.BotDetection/docs/signals-and-custom-filters.md).

---

## Testing It

```bash
# Normal browser request → low bot score
curl -H "Accept: text/html" -H "Accept-Language: en-US" \
  -A "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0.0.0" \
  http://localhost:5090/

# Googlebot → allowed where AllowSearchEngines=true
curl -A "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)" \
  http://localhost:5090/products

# Scraper → blocked by .BlockBots()
curl -A "Scrapy/2.7" http://localhost:5090/api/data

# Full detection breakdown → shows all signals and per-detector contributions
curl http://localhost:5090/bot-detection/check

# Simulate bot types via test mode header
curl -H "ml-bot-test-mode: malicious" http://localhost:5090/bot-detection/check
curl -H "ml-bot-test-mode: scraper" http://localhost:5090/api/data
```

The `/bot-detection/check` endpoint is your development friend. It returns every signal from every detector, timing data, and per-detector contributions so you can see exactly what's happening.

---

## How It Scales: From File to Full Stack

This is the design principle that matters most: **every tier uses the same detection pipeline**. You're never rewriting protection code. You're adding infrastructure around the same core.

### Tier 1: Self-Contained (Where You Start)

```
Your App + AddBotDetection()
    └── SQLite (auto-created botdetection.db)
    └── In-process [HNSW](https://en.wikipedia.org/wiki/Hierarchical_navigable_small_world_graphs) similarity search
    └── 21 detectors, <1ms per request
    └── No external services
```

All 21 detectors run in a wave-based pipeline. Fast-path detectors (UserAgent, Header, IP, Behavioral, TLS fingerprint) execute in parallel in Wave 0. Heuristic scoring extracts ~50 features and runs a lightweight scoring model. Learned patterns persist to SQLite across restarts. If you want internals, Part 2 covers the architecture and signal flow in detail.

**Good for:** Single app, <100K requests/day, getting started.

### Tier 2: Add GeoDetection

Add geo routing plus the geo contributor:

```csharp
builder.Services.AddBotDetection();
builder.Services.AddGeoRoutingWithDataHub(); // free local GeoIP DB (no account)
builder.Services.AddGeoDetectionContributor(options =>
{
    options.FlagVpnIps = true;
    options.FlagHostingIps = true;
});
```

If IP geolocation is new: [GeoIP background](https://en.wikipedia.org/wiki/Geolocation_software#IP_address) and [DataHub GeoIP dataset](https://datahub.io/core/geoip2-ipv4) are good starting points. DataHubCsv downloads a free ~27MB IP database on first run and keeps it updated weekly. All lookups are local - no per-request HTTP calls. For city-level precision, use [MaxMind GeoLite2](https://dev.maxmind.com/geoip/geolite2-free-geolocation-data/).

Now you get 20+ geo signals (country, VPN, proxy, Tor, datacenter detection) and bot origin verification (Googlebot from a Chinese datacenter = suspicious). All the `BlockCountries`, `BlockVpn`, `BlockDatacenter`, `BlockTor` parameters activate.

### Tier 3: PostgreSQL + TimescaleDB

Replace SQLite with PostgreSQL for multi-server shared learning and add [TimescaleDB](https://docs.timescale.com/) (a PostgreSQL extension for time-series data) for analytics:

```csharp
builder.Services.AddBotDetection();
builder.Services.AddStyloBotDashboard();
builder.Services.AddStyloBotPostgreSQL(connectionString, options =>
{
    options.EnableTimescaleDB = true;
    options.RetentionDays = 90;
    options.CompressionAfter = TimeSpan.FromDays(7);
});
```

```yaml
# docker-compose.yml
services:
  timescaledb:
    image: timescale/timescaledb:latest-pg16
    environment:
      POSTGRES_DB: stylobot
      POSTGRES_USER: stylobot
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - timescale-data:/var/lib/postgresql/data

  app:
    build: .
    environment:
      ConnectionStrings__BotDetection: "Host=timescaledb;Database=stylobot;Username=stylobot;Password=${DB_PASSWORD}"
    depends_on:
      timescaledb:
        condition: service_healthy
```

TimescaleDB gives you hypertable partitioning, automatic compression (90-95% storage reduction after 7 days), continuous aggregates for sub-millisecond dashboard queries, and retention policies.

**Good for:** >100K requests/day, multiple servers, need analytics dashboard.

### Tier 4: Full Stack - Gateway + Qdrant + LLM

```
Internet → Caddy (TLS) → Stylobot Gateway ([YARP](https://microsoft.github.io/reverse-proxy/)) → Your App
                              │
                              ├── TimescaleDB (analytics, learning)
                              ├── [Qdrant](https://qdrant.tech/documentation/) (vector similarity search)
                              └── LLamaSharp CPU [LLM](https://en.wikipedia.org/wiki/Large_language_model) (bot classification)
```

The gateway is a standalone Docker container (`scottgal/stylobot-gateway`) that runs detection on all traffic and forwards results as HTTP headers. Your app reads headers - **no SDK needed, any language**. If "gateway" is unfamiliar, think "[reverse proxy](https://en.wikipedia.org/wiki/Reverse_proxy) that sits in front of your app and adds security/traffic logic."

```yaml
services:
  gateway:
    image: scottgal/stylobot-gateway:latest
    environment:
      DEFAULT_UPSTREAM: "http://app:8080"
      StyloBotDashboard__PostgreSQL__ConnectionString: "Host=timescaledb;..."
      StyloBotDashboard__PostgreSQL__EnableTimescaleDB: true
      BotDetection__Qdrant__Enabled: true
      BotDetection__Qdrant__Endpoint: http://qdrant:6334
      BotDetection__Qdrant__EnableEmbeddings: true
      BotDetection__AiDetection__Provider: LlamaSharp
      BotDetection__AiDetection__LlamaSharp__ModelPath: "Qwen/Qwen2.5-0.5B-Instruct-GGUF/qwen2.5-0.5b-instruct-q4_k_m.gguf"

  app:
    build: .
    environment:
      BOTDETECTION_TRUST_UPSTREAM: true

  qdrant:
    image: qdrant/qdrant:latest

  timescaledb:
    image: timescale/timescaledb:latest-pg16

  caddy:
    image: caddy:latest
```

Your app trusts the gateway's headers:

```csharp
// ASP.NET Core
builder.Services.Configure<BotDetectionOptions>(o => o.TrustUpstreamDetection = true);
```

Or read headers directly in any language:

```python
# Python/Flask
@app.route('/api/data')
def api_data():
    if request.headers.get('X-Bot-Detected') == 'true':
        return jsonify(error='blocked'), 403
    return jsonify(data='sensitive')
```

```javascript
// Node.js/Express
app.get('/api/data', (req, res) => {
  if (req.headers['x-bot-detected'] === 'true') {
    return res.status(403).json({ error: 'blocked' });
  }
  res.json({ data: 'sensitive' });
});
```

**Headers the gateway sends:**

| Header | Example | Purpose |
|--------|---------|---------|
| `X-Bot-Detected` | `true` | Bot/human classification |
| `X-Bot-Confidence` | `0.91` | Detection confidence |
| `X-Bot-Detection-Probability` | `0.87` | Bot probability |
| `X-Bot-Type` | `Scraper` | Bot category |
| `X-Bot-Name` | `AhrefsBot` | Identified bot |
| `X-Bot-Detection-RiskBand` | `High` | Risk classification |

### What Each Component Adds

| Component | What It Does | Required? |
|-----------|-------------|-----------|
| **TimescaleDB** | Time-series analytics, compressed storage, continuous aggregates, retention policies | Recommended for production |
| **Qdrant** | Vector similarity search - finds bots even when they rotate User-Agents | Optional |
| **LLamaSharp** | CPU-only LLM for bot cluster naming and classification synthesis | Optional |
| **Caddy/Nginx** | TLS termination, static files | Your existing reverse proxy |
| **Gateway** | Centralized detection for multi-app or non-.NET backends | For multi-service architectures |

### Choosing Your Tier

```
Starting out?
├── Single ASP.NET app → Tier 1 (two lines of code)
│   └── Need geo blocking? → Tier 2 (one more line)
│       └── Need analytics? → Tier 3 (add PostgreSQL)
└── Multiple apps or non-.NET? → Tier 4 (Gateway)
```

Moving between tiers is a DI registration change. Your endpoint protection code - the `[BlockBots]` attributes, the `.BlockBots()` filters, the `context.IsBot()` checks - stays exactly the same.

---

## Enterprise Hooks

The two-line setup is the starting point. Here's what else is built in for production use.

### Response Headers for Debugging

Turn on detection headers globally so you can verify behavior without hitting diagnostic endpoints:

```json
{
  "BotDetection": {
    "ResponseHeaders": {
      "Enabled": true,
      "HeaderPrefix": "X-Bot-",
      "IncludeConfidence": true,
      "IncludeDetectors": true,
      "IncludeProcessingTime": true,
      "SkipPaths": ["/health"]
    }
  }
}
```

Every response gets `X-Bot-Detected`, `X-Bot-Confidence`, `X-Bot-Processing-Ms`, etc. Useful for edge routing decisions in Caddy/Nginx, and for debugging in dev. Disable in production or restrict to trusted networks.

### Challenge Policies (Friction Before Block)

Don't block on uncertainty - challenge instead. StyloBot has five built-in challenge types:

```json
{
  "BotDetection": {
    "ActionPolicies": {
      "challenge-on-uncertain": {
        "Type": "Challenge",
        "ChallengeType": "JavaScript"
      },
      "captcha-gate": {
        "Type": "Challenge",
        "ChallengeType": "Captcha",
        "RedirectUrl": "/captcha"
      },
      "proof-of-work": {
        "Type": "Challenge",
        "ChallengeType": "ProofOfWork"
      }
    }
  }
}
```

Challenge types: `Redirect` (send to challenge page), `Inline` (HTML interstitial), `JavaScript` (JS proof-of-work), `Captcha`, `ProofOfWork` (computational challenge). Assign to endpoints via `[BotPolicy]`:

```csharp
[BotPolicy("default", ActionPolicy = "challenge-on-uncertain")]
public IActionResult Submit() => Ok();
```

### IP Allow/Deny Lists

Global allow and deny lists for known IPs and [CIDR](https://en.wikipedia.org/wiki/Classless_Inter-Domain_Routing) ranges:

```json
{
  "BotDetection": {
    "WhitelistedIps": ["203.0.113.10/32", "198.51.100.0/24"],
    "BlacklistedIps": ["1.2.3.4", "5.6.7.0/24"]
  }
}
```

Whitelisted IPs skip detection entirely. Blacklisted IPs get immediately blocked. Both support CIDR notation.

### OpenTelemetry Metrics

StyloBot exposes metrics via `System.Diagnostics.Metrics`, compatible with [OpenTelemetry](https://opentelemetry.io/docs/), [Prometheus](https://prometheus.io/docs/introduction/overview/), [Grafana](https://grafana.com/docs/), and any .NET metrics consumer.

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics.AddMeter("Mostlylucid.BotDetection"));
```

**Available metrics:**

| Metric | Type | What It Measures |
|--------|------|-----------------|
| `botdetection.requests.total` | Counter | Total requests processed |
| `botdetection.bots.detected` | Counter | Requests classified as bots |
| `botdetection.humans.detected` | Counter | Requests classified as human |
| `botdetection.errors.total` | Counter | Detection pipeline errors |
| `botdetection.detection.duration` | Histogram | Detection latency (ms) |
| `botdetection.confidence.average` | Gauge | Rolling average confidence |
| `botdetection.cache.patterns.count` | Gauge | Cached pattern count |

These are the numbers you need for dashboards, alerting, and capacity planning. Detection latency histogram lets you set SLOs. Bot/human counters give you traffic composition over time.

### Route Group Defaults

Apply bot protection to entire route groups instead of repeating per-endpoint:

```csharp
// All /api routes: block bots, allow search engines
var api = app.MapGroup("/api").WithBotProtection(allowSearchEngines: true);
api.MapGet("/products", () => "data");
api.MapGet("/categories", () => "cats");

// Secured routes: humans only
var secure = app.MapGroup("/secure").WithHumanOnly();
secure.MapPost("/submit", () => "ok");
secure.MapPost("/checkout", () => "done");

// Individual endpoints can still override
api.MapGet("/special", () => "overridden")
   .BlockBots(allowSearchEngines: true, allowSocialMediaBots: true);
```

`WithBotProtection()` takes the same geo/network/confidence parameters as `.BlockBots()`, but intentionally always blocks scrapers and malicious bots at group level (no `allowScrapers` / `allowMaliciousBots` on groups). `WithHumanOnly()` is the group equivalent of `.RequireHuman()`.

### Named Policies on Minimal API

Use `.BotPolicy()` to assign named action policies to Minimal API endpoints - the same thing `[BotPolicy]` does for MVC:

```csharp
// Throttle bots on this endpoint
app.MapGet("/api/data", () => "sensitive")
   .BotPolicy("default", actionPolicy: "api-throttle");

// Block with high-confidence threshold
app.MapPost("/api/submit", () => "ok")
   .BotPolicy("strict", actionPolicy: "block", blockThreshold: 0.8);
```

### Feedback API

Report false positives and negatives back to the system via `POST /bot-detection/feedback`:

```bash
# Mark a detection as a false positive (bot detected but was actually human)
curl -X POST http://localhost:5090/bot-detection/feedback \
  -H "Content-Type: application/json" \
  -d '{"outcome": "Human", "notes": "Known partner integration"}'

# Mark a missed bot (human detected but was actually a bot)
curl -X POST http://localhost:5090/bot-detection/feedback \
  -H "Content-Type: application/json" \
  -d '{"outcome": "Bot", "notes": "Automated scraper spotted in logs"}'
```

The endpoint returns whether the feedback represents a false positive or false negative relative to the current detection result. This is the foundation for closed-loop learning.

### Gateway Trust Boundary with HMAC Signing

When using the YARP gateway, your backend trusts upstream detection headers. This is a security-sensitive setting - you must ensure only the gateway can set those headers.

**Basic trust (network-level isolation only):**

```json
{
  "BotDetection": {
    "TrustUpstreamDetection": true
  }
}
```

**HMAC-signed trust (cryptographic verification):**

```json
{
  "BotDetection": {
    "TrustUpstreamDetection": true,
    "UpstreamSignatureHeader": "X-Bot-Signature",
    "UpstreamSignatureSecret": "base64-encoded-shared-secret"
  }
}
```

When `UpstreamSignatureHeader` and `UpstreamSignatureSecret` are set, the middleware verifies an [HMAC-SHA256](https://datatracker.ietf.org/doc/html/rfc2104) signature before trusting upstream headers.

Use this currently for **custom gateway/proxy integrations** that add signing headers. The built-in Stylobot gateway forwards bot-detection headers, but does not emit HMAC signature headers yet.

Required signed headers:
- `X-Bot-Signature` (base64 HMAC)
- `X-Bot-Detection-Timestamp` (Unix epoch seconds, UTC)

Signing contract:
- `payload = X-Bot-Detected + ":" + X-Bot-Confidence + ":" + X-Bot-Detection-Timestamp`
- `signature = Base64(HMACSHA256(payload, base64Decoded(UpstreamSignatureSecret)))`

Signatures outside a 5-minute replay window are rejected. If the signature is missing, invalid, or expired, upstream headers are rejected and full local detection runs instead.

**Important:** Only enable trust when your backend is behind a trusted reverse proxy. If an attacker can reach your backend directly, they can spoof `X-Bot-Detected: false` and bypass all detection. In production:
- Ensure the backend is not publicly accessible (Docker internal network, Kubernetes ClusterIP)
- Strip `X-Bot-*` headers at your edge proxy before they reach the gateway
- Use HMAC signing for defense-in-depth even with network isolation

---

## What StyloBot Is Not

Worth being explicit:

- **Not a WAF.** StyloBot doesn't inspect payloads for SQL injection or XSS. It identifies *who* is making the request, not *what* they're sending. Use it alongside a WAF, not instead of one.
- **Not a CAPTCHA farm.** Challenge policies exist, but the design philosophy is detection-first. The goal is to know what you're dealing with *before* deciding whether to challenge.
- **Not perimeter-only.** Detection runs per-endpoint with per-endpoint policies. You can have `/products` allow search engines while `/api/checkout` requires humans. This is endpoint semantics, not firewall rules.
- **Not cloud-dependent.** Everything runs self-contained. Qdrant, TimescaleDB, LLM - all optional. The core is two lines of code and a SQLite file.
- **Uncertainty-aware.** Two independent scores (probability + confidence) mean you can distinguish "probably a bot, we're sure" from "probably a bot, but we're guessing". Most systems give you one number and hope for the best.

---

## What's Next

Part 1 covered why bot detection matters. Part 2 covered the detection pipeline internals. This post covered the minimum viable integration and the scaling path - from two lines of code to a full production gateway.

**Get started:**
- NuGet: `dotnet add package Mostlylucid.BotDetection`
- Gateway Docker: `docker pull scottgal/stylobot-gateway`
- [Full docs](https://github.com/scottgal/LLMApi/tree/main/Mostlylucid.BotDetection/docs)
- [Live demo: StyloBot.net](https://stylobot.net)
