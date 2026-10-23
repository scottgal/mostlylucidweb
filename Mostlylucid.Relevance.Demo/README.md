# Fieldwork: Relevance studio

A local shop and operations demo backed by the same in-process Relevance engine. Reuses the `Product` model from `Mostlylucid.AudienceSegmentation.Demo`; does not require that demo's Ollama/Qdrant services or SegmentCommerce's PostgreSQL database.

```sh
dotnet run --project Mostlylucid.Relevance.Demo
```

Open <http://127.0.0.1:5098>. SQLite files are created in the ignored `App_Data` directory. To use an isolated database location, set `Demo__DataPath`. Repeated starts reuse the configured model and idempotent synthetic seed history.

## The shop

- 84 generated products across outdoors, electronics and home, with four measured colour swatches.
- Category placements, price filters and category/price/colour retrieval heads combined with RRF.
- Views, comparisons, cart additions/removals, simulated purchase and cancellation events.
- Recommendation reasons available on each pick; explicit purchase feedback updates head reliability.
- Destination estimates with support and unresolved mass. Seeded histories are synthetic and estimates are uncalibrated.
- Fresh-visit control and a bounded local session profile; no login or payment.

## Behind the shop

Request middleware records local HTTP duration/status and thread-pool backlog. The log-monitor hosted service turns a rolling window into raw system-state features and sends them through Relevance. Normal requests have no application retry loop, so reported retries are zero; sample incidents supply explicit synthetic retry/queue measurements. The UI labels synthetic records.

Replay a retry storm, slow database, queue backlog or recovery trace. Inspect matched diagnostic candidates and finite-horizon destination estimates. State labels are defined by the demo's transparent thresholds; these are not automatically discovered clusters.

Applying the eligible rate action changes the **actual local shop API** limit from 240 to 60 requests/minute. It is one shared demo bucket, not a production per-customer limiter. Operations controls remain available and reset restores the default. Other registered handlers return an inspection snapshot. Nothing alters external services or connection pools.

The intervention notebook records the original recommendation/context and compares pre-action metrics with current observations. Replaying recovery after an action does not establish causality. The [Part 3 learning design](../docs/relevance/part3-intervention-learning.md) describes the separate controlled-intervention extension.

The app binds to loopback by default and is a demonstration host, not a deployed authenticated sidecar. The library itself remains usable without HTTP.

## Architecture

One bounded Relevance worker serves the two engines; one log-monitor hosted service processes structured observations. Shutdown drains logs before stopping the engine worker. SQLite persists product representations, state, transitions, decision attribution and deduplication records. No neural network, external model service or runtime download is needed.

Core API: [Mostlylucid.Relevance](../Mostlylucid.Relevance/README.md). Research and remaining work: [Part 2 plan](../docs/relevance/part2-implementation-plan.md).
