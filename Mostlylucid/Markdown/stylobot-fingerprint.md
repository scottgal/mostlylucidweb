# StyloBot Release Series: Behaviour, Not Identity

*Identity-based bot detection (IPs, user-agents, headers) collapses the moment automation starts rotating identities. StyloBot models clients as behavioural shapes in a 130+ dimensional vector space - here's why that's the right level of abstraction for level-4 and level-5 bots, and how the engine actually works.*

> ## DRAFT
> This is a working draft in the StyloBot Release Series. Structure, examples, and naming may still change before final release.

> **StyloBot Release Series**
>
> 1. **Behaviour, Not Identity** - why StyloBot models clients behaviourally
> 2. [**Behaviour-Aware ASP.NET UI**](/blog/behaviour-aware-ux) - the server-rendered surface over that detection result
> 3. [**Finding and Fixing Unbounded Growth in Long-Running .NET Services**](/blog/stylobot-release-reliability) - the reliability discipline that keeps the engine boring in production
> 4. [**Behaviour-Aware TypeScript UI**](/blog/typescript-sdk) - Express, Fastify, and browser components
> 5. [**The Sidecar Architecture**](/blog/sidecar-architecture) - how the detection engine connects to non-.NET stacks


# Introduction
Most bot systems are still built around identity claims: IP reputation, user-agent strings, header correctness, maybe a fingerprint if you are lucky.

That works right up until the bots get good.

The moment automation starts rotating identities, mimicking browsers, spreading across residential IPs, and adapting in-session, "who does this request claim to be?" stops being the right question. The more useful question is: "what does this client behave like over time?"

That is the idea behind StyloBot. It models requests, sessions, and repeat clients as behavioural shapes rather than static identities. This post is the first entry in the release series and explains that model: why it exists, why it matters, and why behaviour is a better foundation than identity when the bots get smart.

<!--category-- ASP.NET, Bot Detection, Security, Architecture -->
<datetime class="hidden">2026-05-01T10:30</datetime>

# Quick Start
StyloBot is ENTIRELY FREE TO RUN. In future I'll sell realtime management and reporting (to try and you know...eat) but the engine in the exe IS StyloBot. Commercial just adds distributed topology, *realtime* config (no reload), and more DB options.

All the source is here https://github.com/scottgal/stylobot

To install it:
**macOS (Homebrew)**
```bash
brew install scottgal/stylobot/stylobot
stylobot 5080 http://localhost:3000
```

**Linux (apt - Debian/Ubuntu)**
```bash
curl -1sLf 'https://dl.cloudsmith.io/public/mostlylucid/stylobot/setup.deb.sh' | sudo bash
sudo apt update && sudo apt install stylobot
stylobot 5080 http://localhost:3000
```

**Linux (manual / ARM64)**
```bash
# Download from GitHub Releases: stylobot-linux-x64.tar.gz or stylobot-linux-arm64.tar.gz
tar xzf stylobot-linux-x64.tar.gz && chmod +x stylobot && sudo mv stylobot /usr/local/bin/
stylobot 5080 http://localhost:3000
```

**Docker**
```bash
docker run --rm -p 8080:8080 -e DEFAULT_UPSTREAM=http://host.docker.internal:3000 \
  scottgal/stylobot-gateway:latest
```

**NuGet (embed as ASP.NET Core middleware)**
```bash
dotnet add package mostlylucid.botdetection
dotnet add package mostlylucid.botdetection.ui
```

```csharp
builder.Services.AddStyloBot(dashboard => {
    dashboard.AllowUnauthenticatedAccess = true; // dev only
});

app.UseRouting();
app.UseStyloBot();   // broadcast, detection, dashboard: correct ordering guaranteed
app.MapControllers();
```

Dashboard at `/_stylobot`. Detection at `~150µs` per request from first request.

<!-- IMAGE PLACEHOLDER: StyloBot dashboard landing screenshot. Source: screenshot of /_stylobot on a live install (the one with the live request feed + verdict colours). -->
![StyloBot dashboard overview](img-placeholder-dashboard.png?width=900&format=webp&quality=40)

---

Then just run it `stylobot 5080 http://localhost:3000` and voila your upstream site is *listening* (use --mode block to actually block too).

This post is the first entry in the current StyloBot release series. It explains the behavioural model. The next post, [Behaviour-Aware ASP.NET UI](/blog/behaviour-aware-ux), shows how that model becomes application logic inside Razor views and controllers.


[TOC]

# The Threat: a ladder, not a population
Before we look at what defenders have, look at what they're defending against. 'Bots' isn't one thing; it's a ladder, and every rung defeats a different layer of the stack.

<!-- IMAGE PLACEHOLDER: Bot sophistication ladder visualisation, 5 rungs from "curl scripts" up to "LLM-directed adaptive". Source: AI-generated diagram (cleanest if you ask for "5 stylised silhouettes ascending steps, each more humanoid, sci-fi engineering blueprint style"). -->
![Bot sophistication ladder](img-placeholder-bot-ladder.png?width=900&format=webp&quality=40)

---

### 1. Dumb / noisy bots
(curl, scanners, brute force, invalid paths)

* **Fail2Ban:** works well
* **WAF:** works well
* **Bot management:** trivial
* **Rate limiting:** works well

**Failure point:** none, everything catches these.

Scripts that have been around since the dawn of the web (perl FTW!); 'go to site, scrape content'. EASY to identify; single endpoint, same IP, same UA.

---

### 2. Basic scripted bots
(rotating UA, valid endpoints, simple scraping)

* **Fail2Ban:** starts failing
* **WAF:** still effective
* **Bot management:** effective
* **Rate limiting:** depends on tuning

**Failure point:** systems relying on obvious mistakes.

Harder. You now need to identify known patterns and process traffic later.

---

### 3. Headless browser bots
(Puppeteer/Playwright, JS execution, real flows)

* **Fail2Ban:** ineffective
* **WAF:** limited
* **Bot management:** primary layer
* **Rate limiting:** weakening

**Failure point:** anything based on request correctness or signatures.

Easy ONLY because they're often used legitimately (scraping, SEO, etc); this is false-positive city. Telling legit from illegitimate is HARD.

---

### 4. Stealth bots
(proxy rotation, residential IPs, fingerprint spoofing)

* **Fail2Ban:** ineffective
* **WAF:** largely ineffective
* **Bot management:** starts to struggle
* **Rate limiting:** ineffective if distributed

**Failure point:**

* IP reputation
* static fingerprinting
* threshold-based controls

This is where false positives spike. Push harder and your normal identifiers fall off; *you need to identify the same client through a deceptive identity*.

---

### 5. Adaptive / LLM-directed bots
(slow, distributed, learn site behaviour, adjust dynamically)

* **Fail2Ban:** irrelevant
* **WAF:** ineffective
* **Bot management:** inconsistent
* **Rate limiting:** ineffective

**Failure point:**

* anything assuming repeatability
* anything assuming known patterns
* anything assuming "bot-like" behaviour

These bots behave "correctly" and evolve. LLMs adapt to standard blocking attempts (CAPTCHA solvers, randomizers).

THIS is where StyloBot is aimed. Right NOW these bots are expensive to run at scale. THAT IS CHANGING.

---

We've moved through time as well as up the ladder; from simple identity (block IP) to needing to understand huge volumes of traffic and log files. **To defend against INTELLIGENT scrapers at level 5 you need INTELLIGENT detection AND protection.**

# The Defenders: the current market
With the ladder in mind, here's the kit defenders bring. Notice how every option is tuned for somewhere between level 1 and level 3.

<!-- IMAGE PLACEHOLDER: Heatmap, rows = the 9 market categories below, columns = bot levels 1-5, cells coloured green→red as effectiveness drops. Source: hand-drawn in something like Excalidraw / Figma; this should be a YOU diagram, not AI, since the calls are subjective. -->
![Defender effectiveness vs bot level](img-placeholder-defender-heatmap.png?width=900&format=webp&quality=40)

---

### 1. Fail2Ban / log-based banning

* **Mode:** Post (reactive)
* **Latency:** seconds → minutes
* **Cost:** very low (free + ops time)
* **Complexity:** low

> Cheap, simple, but always after the fact

---

### 2. WAF (Cloudflare WAF, AWS WAF, Azure WAF)

* **Mode:** Active (inline)
* **Latency:** ~1–10 ms
* **Cost:** low → medium (rules + request volume)
* **Complexity:** low → medium (rule tuning)

> Fast and cheap-ish, but only for known patterns

---

### 3. Bot Management (Cloudflare Bot Mgmt, DataDome, HUMAN, Akamai, CHEQ)

* **Mode:** Active (inline + challenges)
* **Latency:** ~5–50 ms
* **Cost:** medium → high (often traffic-based or tiered)
* **Complexity:** medium → high (tuning, false positives, UX impact)

> Powerful but expensive, and can affect user experience

---

### 4. Rate Limiting / API Gateway controls

* **Mode:** Active (inline)
* **Latency:** ~1–5 ms
* **Cost:** low → medium (usually bundled but scales with usage)
* **Complexity:** medium (per-endpoint tuning)

> Cheap control, but blunt instrument

---

### 5. DDoS Protection (Cloudflare, Akamai, Fastly, AWS Shield)

* **Mode:** Active (edge/network)
* **Latency:** ~1–5 ms
* **Cost:** medium → very high (especially at scale / enterprise tiers)
* **Complexity:** medium (mostly managed)

> Essential infra layer, but not behavioural

---

### 6. Fraud / Risk Scoring (Sift, Forter, Riskified, Stripe Radar)

* **Mode:** Mixed (inline + post)
* **Latency:** ~50–300 ms inline
* **Cost:** high (per transaction / % of revenue / SaaS pricing)
* **Complexity:** high (integration + tuning + ops)

> Deep insight, but slow and expensive...used sparingly

---

### 7. Device Fingerprinting (FingerprintJS, ThreatMetrix, iovation)

* **Mode:** Active (client + inline)
* **Latency:** ~10–100 ms
* **Cost:** medium → high (per request/session pricing)
* **Complexity:** high (privacy, evasion, integration)

> Identity-heavy, comes with compliance and cost baggage

---

### 8. SIEM / Observability (Splunk, Datadog, Elastic, Sentinel)

* **Mode:** Post
* **Latency:** seconds → minutes
* **Cost:** very high (data ingestion is the killer)
* **Complexity:** very high (queries, alerts, maintenance)

> Visibility layer...expensive but necessary

---

### 9. Custom glue / edge logic / lambdas

* **Mode:** Mixed
* **Latency:** varies
* **Cost:** hidden but real (dev time + infra)
* **Complexity:** high over time

> The "we had to fix gaps" layer

---

# THE BIG PROBLEM
Look at that list against the ladder. Every category needs UA / IP to remain identifiable, or needs manual config per endpoint to avoid blocking 'legitimate' traffic. They're SLOW; if every request goes through this pipeline that's a significant chunk of your time spent processing requests instead of responding to them. And no matter how much you spend, past a certain point you won't block the level-5 bots AND you'll be spending more than you save.

The market covers MOST of the bases for bots at levels 1–3. It doesn't have an answer for level 5.

# Potential Solution: behavioural inference
In previous articles I've written about my [Behavioural Inference](/blog/behavioural-inference-systems-blog) systems. They're a CHEAT that became a feature.

Single 'sensors' are easy to bypass now. The only constant in level 4-5 attacks is *how they deceive*; rotating headers, IPs, UAs, timings, endpoints. Any ONE sensor can be bypassed. Combining sensors raises sensitivity (catches more bots) but in static systems it also raises false positives, because if a single trigger is enough to block, every added sensor is another way to misfire.

Behavioural inference does three things: *profile* -> *characterize* -> *remember*. That's it; whether it's lucidRAG or StyloBot. In StyloBot what gets remembered is a behavioural *vector*; that's what a client *behaviour* becomes. Note behaviour, NOT identity.

To StyloBot you are a projection over a 130+ dimensional vector space.

# StyloBot
StyloBot is a behavioural inference engine applied to web traffic. It uses a large vector space to characterise web requests and identify automation vs humans.

It's closer to a sensor fusion system than a rules engine. Detectors don't make decisions; they emit *signals*. The signals are the system. Detectors are just producers.

## How it differs from the market
The market leaders share one of two shapes; either they rely on simple static rules (updated constantly, like OWASP feeds) or they analyse TONS of real traffic and need a SaaS to live in.

StyloBot aims for the distribution model of Fail2Ban (run an exe, point at upstream) with the power of the enterprise stacks. It downloads lists of user agents, CVEs, exploits, and other indicators of compromise to enrich detection; but those are one factor in a decision, never the verdict on their own.

Under the hood StyloBot runs ~50 'contributors'; small focused bits of code that look like this:

```csharp 
using Microsoft.AspNetCore.Http;
using Mostlylucid.BotDetection.Models;

namespace Mostlylucid.BotDetection.Detectors;

/// <summary>
///     Execution stage for detectors. Detectors in the same stage run in parallel.
///     Higher stages wait for lower stages to complete.
/// </summary>
public enum DetectorStage
{
    /// <summary>
    ///     Raw signal extraction (UA, headers, IP, client-side).
    ///     No dependencies on other detectors.
    /// </summary>
    RawSignals = 0,

    /// <summary>
    ///     Behavioral analysis that may depend on raw signals.
    ///     Runs after Stage 0 completes.
    /// </summary>
    Behavioral = 1,

    /// <summary>
    ///     Meta-analysis layers (inconsistency detection, risk assessment).
    ///     Reads signals from stages 0 and 1.
    /// </summary>
    MetaAnalysis = 2,

    /// <summary>
    ///     AI/ML-based detection that can use all prior signals.
    ///     Runs last, can learn from all other signals.
    /// </summary>
    Intelligence = 3
}

/// <summary>
///     Interface for bot detection strategies
/// </summary>
public interface IDetector
{
    /// <summary>
    ///     Name of the detector
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Execution stage for this detector.
    ///     Detectors in the same stage run in parallel.
    ///     Higher stages wait for lower stages to complete.
    /// </summary>
    DetectorStage Stage => DetectorStage.RawSignals;

    /// <summary>
    ///     Analyze an HTTP request for bot characteristics.
    ///     Legacy method - prefer DetectAsync with DetectionContext.
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detection result with confidence score and reasons</returns>
    Task<DetectorResult> DetectAsync(HttpContext context, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Analyze an HTTP request for bot characteristics using shared context.
    ///     Detectors should read signals from prior stages and write their own signals.
    /// </summary>
    /// <param name="detectionContext">Shared detection context with signal bus</param>
    /// <returns>Detection result with confidence score and reasons</returns>
    Task<DetectorResult> DetectAsync(DetectionContext detectionContext)
    {
        // Default implementation for backward compatibility
        return DetectAsync(detectionContext.HttpContext, detectionContext.CancellationToken);
    }
}

/// <summary>
///     Result from an individual detector
/// </summary>
public class DetectorResult
{
    /// <summary>
    ///     Confidence score from this detector (0.0 to 1.0)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    ///     Reasons found by this detector
    /// </summary>
    public List<DetectionReason> Reasons { get; set; } = new();

    /// <summary>
    ///     Bot type if identified
    /// </summary>
    public BotType? BotType { get; set; }

    /// <summary>
    ///     Bot name if known
    /// </summary>
    public string? BotName { get; set; }
}
```

Each detector declares what it is, what it depends on, and what it returns.

> NOTE: This is a core concept. StyloBot is a LARGE system with MINIMAL concepts; adding detectors is SIMPLE.

That stage ordering is the discipline. Stage 0 runs in parallel and writes signals. Stage 1+ reads what came before instead of re-extracting from the raw request. Most requests never get past stage 0.

Using my [mostlylucid.ephemeral framework](https://github.com/scottgal/mostlylucid.atoms) (more on it in [Building a Reusable Ephemeral Execution Library](/blog/ephemeral-execution-library) and [Ephemeral Signals - Turning Atoms into a Sensing Network](/blog/ephemeral-signals)) detectors emit what I call 'signals'; tiny strings like `ua.score=0.75` that act as both metadata for the request AND logging / diagnostic data. The Code LLM (and the system itself) uses these signals to identify efficiencies; auto-tuning.

> Aside: Ephemeral also gives StyloBot LFU / sliding-window processing; it drops human requests while retaining a window so that if a *future* request crosses a bot threshold we can look back and reprocess the older ones for clues. That mechanism deserves its own post; for now just know it's why retention costs nothing in the steady state.

## The 50 detectors aren't 50 decisions
StyloBot has 50 detectors. It rarely runs more than 5-7 per request. They aren't 50 independent verdicts; they're 50 ways of observing the same underlying behaviour, each contributing evidence toward a single behavioural model.

The 50 is the CAPABILITY; it only uses what it needs.

**Fast path (the common case).** 5-7 SUPER fast (sub-millisecond) initial detectors and fingerprinters. From that fingerprint it can decide what sort of thing you are AND what your next requests are likely to be (content->resource pathing). Then it predicts the next request, compares against what actually arrives, and escalates only if the shape diverges. ~150µs end to end. This is what the vast majority of human traffic ever sees.

**Slow path (the interesting case).** Crucially, **the slow path runs OUT of the request pipeline**. Your user's response goes out on the fast-path verdict; the slow path is enrichment for what happens next, not latency on this request.

It triggers when the fast path is *ambiguous* (signals contradict each other, the shape doesn't match anything we've seen, confidence sits in the dead zone) or when the request looks novel (new attack pattern, fresh CVE probe, an LLM-driven scraper trying something we haven't fingerprinted yet). When it does, StyloBot opens the throttle. ALL 50 detectors run. The Intelligence stage consults an LLM that takes the full signal bundle and contributes another dimension of resolution; pattern-matching against threat intel, reasoning about request intent, spotting things the heuristics aren't shaped for yet.

You get two escalation options:

* **Inline escalation** (still off the request thread; runs in the background but writes a verdict before the *next* request from the same client lands). Good for short flows where you want the next click already classified.
* **Offline escalation** (batched, runs on a worker; the verdict shows up seconds-to-minutes later and updates the cluster / reputation store). Good for long-tail enrichment, periodic sweeps, and the cases where you'd rather pay nothing on the hot path.

Either way, the request that triggered the escalation already responded. There is no scenario where the slow path adds milliseconds to a user's page load.

That's the deal: pay microseconds when you can, pay milliseconds when you must, never pay both, and never pay them on the user's clock. The slow path is rare by design (typically <1% of traffic) but it's where StyloBot earns its keep against the level-5 adaptive bots from earlier; the ones that *will* slip past any fixed pipeline. Every slow-path verdict feeds back as new fast-path signal, so next time the cheap detectors catch what the expensive ones discovered.

The full set of layers (you only see all of them on a slow-path request that genuinely needs every angle):


| Layer | Detectors | What it catches |
|-------|-----------|-----------------|
| **Identity** | Signature, HeaderCorrelation, Periodicity | UA rotation, identity factors, temporal patterns |
| **Protocol** | TLS (JA3/JA4), TCP/IP (p0f), HTTP/2, HTTP/3, Transport, StreamAbuse | Spoofed browser fingerprints, protocol inconsistencies |
| **Behavioral** | Waveform, SessionVector, AdvancedBehavioral, CacheBehavior, CookieBehavior, ResourceWaterfall, ContentSequence | Timing patterns, Markov chains, missing assets, page-load sequence divergence |
| **Content** | UserAgent, Header, AiScraper, Haxxor, SecurityTool, VersionAge | Known bots, attack payloads, impossible browser versions |
| **Network** | IP, GeoChange, ResponseBehavior, MultiLayerCorrelation, CveProbe | Datacenter IPs, impossible travel, CVE scanning, cross-layer mismatches |
| **Intelligence** | FastPathReputation, ReputationBias, TimescaleReputation, Cluster, Similarity, Intent | Historical reputation, Leiden clustering, HNSW similarity, threat scoring |
| **Ad Fraud** | ClickFraud, PiiQueryString | IAB SIVT: datacenter/VPN/headless on paid traffic, referrer spoofing, immediate bounce |
| **AI** | Heuristic, HeuristicLate, LLM | 50-feature model (<1ms), optional LLM for ambiguous cases |
| **Client** | ClientSide, FingerprintApproval, ChallengeVerification | JS timing probes, headless detection, PoW challenges |

<!-- IMAGE PLACEHOLDER: Fast-path / slow-path flow. CRITICAL: must show fast path returning a response in ~150µs while slow path forks OFF the request thread (dotted arrow into a separate "background / worker" lane) and eventually writes back to the cluster store. Two slow-path lanes: "inline (background, before next request)" and "offline (batched worker)". Source: Excalidraw / Mermaid swimlane diagram. Hand-made beats AI here; the routing logic is exact and the OFF-THREAD nature is the whole point. -->
![Fast path vs slow path (slow path is off the request thread)](img-placeholder-fast-slow-path.png?width=900&format=webp&quality=40)

# What if client behaviour was a vector?
With 50 detectors and hundreds of signals we have a LOT of metadata about each client. None of it on its own is a verdict; together it's a *position* in a 130+ dimensional space.

This is a *projection* ([Wikipedia](https://en.wikipedia.org/wiki/Projection_(linear_algebra))) of that underlying vector space, built from the contributors above. A 'low resolution' image of the fingerprint.

<!-- IMAGE PLACEHOLDER: 2D projection of the high-dimensional behavioural vectors, humans clustered tight in green, bots scattered in distinct shapes/colours. Source: SCREENSHOT from /_stylobot dashboard if it has a UMAP/t-SNE view; if not, generate one server-side from real session data and snapshot it. AI-generated as a last resort with prompt "scientific scatter plot, 2D projection, dense human cluster centre, sparse bot clusters around edges, dark theme". -->
![Behavioural vector projection](img-placeholder-vector-projection.png?width=900&format=webp&quality=40)

## Bots are shapes
Your bots aren't just a bunch of numbers. They're SHAPES. These shapes are DIFFERENT to human ones.

Humans are noisy but *consistent in structure*. Bots are consistent but *wrong in structure*.

That's the whole trick. Once you can see the shape, the per-detector confidence scores stop mattering individually; what matters is whether the projection looks like a human or like something pretending to be one.

Combine that with tracking across ALL sessions (the system collects ZERO PII). A single session might look totally human (it might even *be* a recording of one). HOWEVER...sensitivity across TIME (looking for automated cadences, even human fingerprints which get USED as bots later) is where the shape really gives them away.

## Bots cluster (Leiden over the vectors)
Once everything is a shape, bots stop hiding from each other. They cluster.

StyloBot runs [Leiden community detection](https://en.wikipedia.org/wiki/Leiden_algorithm) over the live vector space. This trick is borrowed wholesale from my GraphRAG work; if you've read [GraphRAG: Why Vector Search Breaks Down at the Corpus Level](/blog/graphrag-knowledge-graphs-for-rag) and [GraphRAG Part 2: Minimum Viable GraphRAG](/blog/graphrag-minimum-viable-implementation) you've already seen this exact pattern. There it builds *communities of meaning* over document chunks so a query can pull a whole connected idea instead of disconnected snippets. Here it does the structurally identical job over *behavioural* vectors; communities of clients that move alike. Same algorithm, same insight, different domain. A bot family is just a community in the graph; a GraphRAG topic is the same shape over text.

Bots that share an origin (same toolkit, same operator, same scraping campaign) land in the same neighbourhood even when they've rotated IPs, headers, fingerprints and timing. They didn't co-ordinate to look the same; they look the same because they ARE the same, structurally.

That gives StyloBot two superpowers for free:

* **A new request gets the verdict of its cluster.** First-ever-seen bot from a known operator? Already inside a hostile community on arrival. No warm-up, no learning period.
* **Novel attacks make their own cluster.** When something genuinely new shows up it doesn't fit anywhere; that *itself* is the signal. The slow path runs, the LLM stage labels it, and from then on the whole cluster is recognised on the fast path.

This is also where similarity search (HNSW over the same vectors) earns its keep; "show me the 20 closest things to this request right now" is a constant-time question, not a scan over history.

<!-- IMAGE PLACEHOLDER: Leiden communities over the behavioural vector graph; nodes coloured by community, with one tight human community and several smaller bot communities at the edges. Source: SCREENSHOT from /_stylobot dashboard if it has a cluster view (it should); otherwise render from a Gephi/networkx export of a real run. AI-generated as a last resort with prompt "force-directed graph, multiple coloured communities, one large central community, several small peripheral ones, dark scientific style". -->
![Leiden communities over behavioural vectors](img-placeholder-leiden-clusters.png?width=900&format=webp&quality=40)

## Odd Implications
Note what I DIDN'T say. I didn't say 'once set up' or 'when properly configured' because that's StyloBot's secret; it has a good default set but *it learns*.

As it runs it profiles *your traffic* and understands *your users*. Not creepily; it works out what request patterns, endpoints, and timings look like for your human vs your automated traffic.

You can THEN decide, or let the system take care of it (set a bot threshold of say 0.8 for most and 0.6 for secure endpoints). The defaults that ship are good; the defaults that emerge after a few hours on your traffic are better.

# Conclusion
StyloBot is NOW live. The detection engine, the dashboard, the NuGet packages, the gateway exe; all of it is shipping right now and FREE to run on your own infra. Grab the source at [github.com/scottgal/stylobot](https://github.com/scottgal/stylobot) or `brew install scottgal/stylobot/stylobot` and point it at your upstream.

I'll add commercial features shortly (managed dashboards, hosted reputation, multi-site reporting; the things that need a server somewhere I have to keep paying for) but the core engine stays free.

Next in the release series: [Behaviour-Aware ASP.NET UI](/blog/behaviour-aware-ux), which takes the behavioural classification described here and exposes it to Razor, forms, and controller policy. After that, [Finding and Fixing Unbounded Growth in Long-Running .NET Services](/blog/stylobot-release-reliability) covers the reliability rework that lets the engine sit on a Pi forever without operator intervention, with the StyloBot vector layer as the worked example.

If you want the older technical lead-up to this release series, [Part 1](/blog/botdetection-introduction), [Part 2](/blog/botdetection-part2-signature-pipeline-and-stylobot-architecture), and [Part 3](/blog/botdetection-part3-as-simple-as-possible) cover the why, the architecture, and the two-line drop-in. The behavioural inference foundations live in [Behavioural Inference](/blog/behavioural-inference-systems-blog), the signal plumbing in [Ephemeral Signals](/blog/ephemeral-signals), and the Leiden / clustering lineage in [GraphRAG](/blog/graphrag-knowledge-graphs-for-rag) and [GraphRAG Part 2](/blog/graphrag-minimum-viable-implementation).
