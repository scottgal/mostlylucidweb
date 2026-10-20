# Real Umami integration tests

Run from the repository root with Docker and the target .NET SDKs installed:

```sh
scripts/test-umami-integration.sh
```

The script starts **Umami 3.4.0 and PostgreSQL 16**, waits for both, runs the suite
against real HTTP endpoints, then removes its containers, database volume and
network. It binds only to `127.0.0.1:18086`, uses disposable credentials and reads
no production settings. An occupied port causes startup to fail; it does not reuse
or delete the existing listener. Failure output includes disposable server logs.

By default all three target frameworks run. To select one or use Release builds:

```sh
scripts/test-umami-integration.sh --framework net10.0 --configuration Release
```

A machine with only the .NET 10 runtime can test the net8/net9 compiled targets
using `DOTNET_ROLL_FORWARD=Major scripts/test-umami-integration.sh`. CI installs
and runs the **native .NET 8, 9 and 10 runtimes**. TRX reports are written under
`Umami.Net.Test/TestResults/integration/` and uploaded by CI.

## What is tested

The **51 real integration cases** use isolated websites and known event/visitor
counts, not HTTP mocks. Each website is removed after its case, even on failure;
account/key resources are removed when the fixture ends. Framework processes
can share the disposable server because their resource IDs are independent.

| Area | Checks |
| --- | --- |
| Collection | All 16 public tracking/identify overload paths; current JSON/cache/session metadata; raw response usability; actual stored page/event types |
| Identity/cache | Repeat visitor with cache, changed identity with the same IP/agent, session properties, no shared-cache contamination |
| Request capture | Real inbound agent, path base/path/query, UTM data, historical seconds-based timestamp |
| Bot handling | Real Umami bot response and confirmation that no page view was saved |
| Background sender | Page, named event, raw event, both identity methods, performance, and persisted results after shutdown drains the queue |
| Stats/active | Exact seeded pageview/visitor counts, both stats overloads, active visitors, empty/out-of-range queries |
| Metrics/series | Both pageview/event-series overloads, minute buckets, expanded rows, path/identity/UTM/search filters, paging, event-name listing |
| Analytics | All 16 `AnalyticsEndpoint` values: attribution, breakdown, funnel, goal, heatmap, journey, retention, performance stats/chart/metrics, UTM, revenue stats/chart/metrics, sessions, events |
| Analytics values | Seeded revenue amount/count, performance percentiles/count, UTM counts, session counts, typed models and session-detail resource |
| Annotations | Create, read, update, list, date filtering, page boundaries, deletion and 404 |
| Saved resources | Goal and funnel create/list/search/read/update/calculate/delete/404 through the generic resource API |
| Authentication | Actual login and POST verify, invalid/expired session refresh, bad credentials, a valid user denied access, bounded reauthentication |
| API keys | Create a real key, read/write/verify, revoke it, prove rejection without password fallback |
| Errors | Real server 400/401/404, rejected collector data, explicit cancellation and no unintended stored event |
| Transport failures | Refused real loopback connection, TCP peer withholding its HTTP response, timeout mapped to 504 versus caller cancellation propagated |

The runner and CI run the whole suite, including all theory rows. The publication
job cannot pack/publish a package after an integration failure. `main`/PR runs do
not publish to NuGet; package publication still requires a `umamiv*` tag.

The guard accepts only a loopback HTTP endpoint on port 18086. For manual debugging
against an independently started **disposable** stack:

```sh
UMAMI_TEST_URL=http://127.0.0.1:18086/ \
  dotnet test Umami.Net.Test/Umami.Net.Test.csproj --framework net10.0 \
  --filter FullyQualifiedName~Umami34IntegrationTests
```

Without that explicit opt-in, integration cases are skipped. Never proxy a
production service onto the test port: this suite creates, edits and deletes
resources using the stock disposable admin account.

Cloud authentication/path conventions, legacy response shapes and malformed
response bodies also have separate deterministic protocol tests. This stack
verifies the self-hosted PostgreSQL backend; it does not claim live paid-Cloud,
ClickHouse, old-server or browser session-recording coverage.
