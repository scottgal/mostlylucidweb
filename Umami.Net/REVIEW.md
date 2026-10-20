# Umami.Net review — 20 October 2026

Reviewed the collection client, payload capture, background delivery, authentication,
analytics methods/models, query serialization, configuration, docs, tests and package
workflow against the latest official release, **Umami 3.4.0**. This review accompanies Umami.Net 1.1.0. The package does not upgrade a deployed
Umami server or database. See [migration notes](MIGRATION.md) for behavior changes.

## Findings and changes

| Finding | Result |
| --- | --- |
| Collector success was treated only as a bare JWT; current JSON responses could not decode | Parse `{ cache, sessionId, visitId }`; retain legacy metadata decoding |
| Bot detection searched arbitrary response text | Recognize the actual `{ "beep": "boop" }` contract |
| Most analytics 401 handlers recursed without a bound | Shared transport, one forced reauthentication retry; revoked keys never fall back to passwords |
| Auth verification used GET; latest server requires POST | Correct method; partial 2FA response is rejected as incomplete login |
| API-key/Cloud access was missing | Current Bearer authentication, allowed key verification route, Cloud URL prefix handling |
| Stats expected nested values; active visitors expected only `x` | Normalize current and legacy shapes without removing existing model properties |
| Metrics blindly changed query strings after any 400 | Explicit version mapping; V3 `url` dimension maps to `path`; preserve filter values |
| Event-name metrics unnecessarily required a specific event | Allow listing all event names |
| Current dimensions and filters were missing or lowercased incorrectly | Add identity/UTM dimensions and filters; preserve camelCase; invariant numeric formatting |
| Minute buckets and event-series default timezone were missing | Add minute units and UTC default for the endpoint that requires a timezone |
| Page/identify event data could be overwritten; request user agent was replaced by default | Preserve data and real request agent/IP/path/query; apply default agent only explicitly |
| Modern collector fields and Web Vitals were unavailable | Tags, seconds-based timestamps, device overrides, per-payload cache tokens, validated performance collection |
| Background queue was unbounded; shutdown cancelled before draining | Bounded queue with backpressure, drain until the host deadline, cancellable HTTP delivery |
| HTTP request/response ownership leaked resources | Dispose internal requests, decoded responses, failed retry responses and background responses |
| Transient retries could duplicate POST writes | Retry only GET requests; explicit writes remain caller-controlled |
| Registration changed global logging; logs could include tokens or response bodies | Preserve host logging configuration; omit tokens, payload bodies and query strings |
| Current annotations and report datasets were unavailable | Typed annotation CRUD/paging; website-scoped analytics GET endpoints and scoped generic resource access |
| `Analytics:User` legacy setting did not bind to Username | Alias supported; explicit Username wins |
| A local props file set `TargetFramework=net9.0` alongside multi-target settings | Remove the single-framework override; verify all three package library folders |
| Release tests ran only on net9 and only after a release tag | Test all three targets; run CI on scoped main/PR changes and manually; publish only for `umamiv*` tags |
| Tests mostly relied on permissive mocks | Add protocol regression cases and an opt-in real Umami 3.4/PostgreSQL test; gate package publication on it |
| README promised auto-detection, unlimited non-blocking delivery and universal version support | Replace with documented configuration, behavior and limits |

Existing public collection and data entry points remain available. New APIs use
`Async` suffixes. Invalid local arguments fail before sending; analytics transport
failures produce bounded status results. The modern analytics helper returns
`JsonElement` by default or a caller-selected model; it does not claim a generated,
strongly typed wrapper for every administrative API or browser replay protocol.

## Verification

- Native .NET 8, 9 and 10 passed 144 unit/contract tests and 51 real-server
  integration cases per target: [run 39432202360](https://github.com/scottgal/mostlylucidweb/actions/runs/39432202360).
  That main-branch run also packed all three targets; publication is gated separately
  by a release tag. Local net8/net9 runs use `DOTNET_ROLL_FORWARD=Major` on .NET 10.
- Disposable official Umami 3.4.0 and PostgreSQL 16 containers verify all 16
  analytics endpoints, stored values, collection/identity/cache, Web Vitals,
  filtering/paging, annotations, saved goals/funnels, permissions, revoked keys,
  background delivery, cancellation and actual socket failures. Each case owns
  an isolated website; test users, keys and the disposable database are cleaned up.
- Contract cases cover Cloud path/auth headers, bounded 401 handling, partial 2FA,
  malformed responses, cancellation, regex and UTM serialization, scoped resource
  paths, cache isolation, performance ranges and background shutdown.
- Release package builds with `lib/net8.0`, `lib/net9.0` and `lib/net10.0` assemblies.
  Local release verification: `dotnet pack Umami.Net/Umami.Net.csproj -c Release -p:MinVerVersionOverride=1.1.0`.
- NuGet vulnerability audit reports no known vulnerable dependencies for this
  package against the configured public source.

## Run the disposable integration check

Use an isolated Docker network with PostgreSQL 16 and
`ghcr.io/umami-software/umami:3.4.0`. The publication workflow uses `scripts/test-umami-integration.sh` for startup,
readiness and cleanup. Wait for PostgreSQL readiness before starting Umami.
The check uses the stock disposable admin credentials and creates test resources;
**never point it at production**. Its URL guard accepts only loopback HTTP on 18086.

```sh
scripts/test-umami-integration.sh --configuration Release
```

Without `UMAMI_TEST_URL`, integration cases are skipped. Other tests need no
Umami service. The in-memory delivery queue is not durable. API keys, annotations,
performance and modern analytics routes depend on the server version and access
permissions. Cloud headers/path behavior are checked with protocol fixtures;
no paid Cloud account was used. Legacy response shapes/mappings are covered by
fixtures; an old Umami server was not deployed during this review. Native .NET 8/9 execution is verified by CI above. The packaged-image website
release is separate from this SDK update.

## Primary sources

- [Umami 3.4.0 release](https://github.com/umami-software/umami/releases/tag/v3.4.0)
- [Collection route and schema](https://github.com/umami-software/umami/blob/v3.4.0/src/app/api/send/route.ts)
- [Analytics request/response schemas](https://github.com/umami-software/umami/blob/v3.4.0/src/app/api/websites/%5BwebsiteId%5D/analytics-schema.ts)
- [Website analytics migration guide](https://github.com/umami-software/umami/blob/v3.4.0/docs/report-api-migration.md)
- [Authentication](https://docs.umami.is/docs/api/authentication)
- [Cloud API key and endpoint](https://docs.umami.is/docs/cloud/api-key)
- [Annotation routes](https://github.com/umami-software/umami/tree/v3.4.0/src/app/api/websites/%5BwebsiteId%5D/annotations)

## Expanded integration suite

The original single smoke scenario is replaced with 51 independent integration
cases. All 51 passed locally for net8.0, net9.0 and net10.0 (153 integration
passes), using the real pinned Umami 3.4.0 image and PostgreSQL 16. No SDK source
fix was needed to make the expanded cases pass. The suite verifies stored values,
all analytics endpoint routes, authentication failures, resource lifecycles,
collection/identity overloads, background delivery and real transport errors.

`./scripts/test-umami-integration.sh` now manages disposable startup/readiness,
test execution, failure diagnostics, TRX reports and cleanup. CI uses this same
runner against all three native runtimes before packing or publishing.
See the [coverage and usage guide](https://github.com/scottgal/mostlylucidweb/blob/umamiv1.1.0/Umami.Net.Test/Integration/README.md) for the
case matrix, isolation guarantees and explicit limits.
