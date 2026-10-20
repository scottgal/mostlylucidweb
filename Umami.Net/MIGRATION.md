# Upgrading to Umami.Net 1.1.0

```sh
dotnet add package Umami.Net --version 1.1.0
```

This release targets .NET 8, 9 and 10 and is tested against self-hosted Umami
3.4.0 with PostgreSQL 16. Existing public tracking and data entry points remain
available. The changes below affect configuration and runtime behavior.

## Choose your server API version explicitly

`Analytics:ApiVersion` now defaults to `V3`. `MetricType.url` is serialized as
`type=path` for V3. The SDK no longer guesses a server version by rewriting a
400 response and resending the request.

For an older server, select `V2` to retain the `url` metric value, or `V1` for
explicit legacy `path` → `url` and `hostname` → `host` parameter mappings. These
mappings do not add newer server capabilities to older installations.

## Authentication

For self-hosted Umami 3.4, set `Analytics__ApiKey` to an account API key. The
SDK sends it as a Bearer credential. Username/password authentication remains
available through `Analytics__Username` and `Analytics__Password`. The old
`Analytics__User` name is accepted as an alias; an explicit Username wins.

Password sessions are verified using POST `/api/auth/verify`. A denied request
forces one fresh login and then stops retrying. A 2FA partial token is treated
as an incomplete login; use an API key for unattended access to 2FA accounts.
Revoked API keys do not fall back to password authentication.

For Cloud analytics, use `UmamiPath=https://api.umami.is/v1/` (or `/v1/eu/`,
`/v1/us/`), `UseCloudApi=true` and `ApiKey`. The SDK preserves the base path and
uses Bearer authentication. Collection uses the tracker host, not this Cloud
data endpoint. No paid Cloud account was used for the integration suite;
Cloud wire conventions are covered by protocol tests.

## Tracking results and visitor metadata

Current collector JSON responses expose `SessionId`, `VisitId` and `Cache`.
`Id` also contains the session ID. Older bare-JWT responses remain supported as
analytics metadata. Use `CacheToken` on a subsequent payload to send the cache
back to Umami; keep it with the corresponding visitor. It is never shared in
the client's default headers.

Use `DistinctId` to identify visitors on current servers. Payload `SessionId`
is retained for legacy callers; current Umami computes session IDs itself.

The SDK now preserves the real inbound user agent, IP override and path base/
path/query string. Consequently UTM query data is retained and actual bot agents
can be filtered by Umami. `UseDefaultUserAgent` remains an explicit override.
Review analytics assumptions if an older integration relied on every visitor
being reported as the SDK's synthetic agent.

## Ownership, retries and backpressure

Dispose responses returned by raw methods, for example:

```csharp
using var response = await client.Track("purchase");
response.EnsureSuccessStatusCode();
```

`*AndDecode` methods own and dispose their HTTP responses. Internal analytics
requests, retries and background responses are disposed by the SDK.

Tracking and other POST/DELETE writes are not automatically retried for transient
errors, avoiding duplicate writes when the acknowledgement is lost. GET requests
have bounded transient/429 retries. Authentication retries are bounded separately.
If tracking must be durable, use an application outbox with your own delivery
policy; the built-in queue is in memory.

Background delivery now holds at most 1,024 items. Enqueue calls wait when it is
full, and shutdown drains queued events until the host's cancellation deadline.
It does not promise unlimited fire-and-forget delivery or survival across crashes.

## Data responses and errors

Stats normalize both legacy nested `{ value, prev }` values and current totals
with a `comparison` object into the existing `.pageviews.value` / `.prev` API.
Active visitors normalize both `x` and current `visitors` fields.

Argument/date validation runs before HTTP requests and can throw; do not rely on
all invalid local input becoming a status result. Server HTTP errors retain their
status. Transport failures map to 503, timeouts to 504 and invalid response JSON
to 502. Explicit caller cancellation propagates as `OperationCanceledException`.

Registration no longer changes the host's logging levels or adds a console
provider. Configure logging in your application as usual. Request logs omit
payloads, cache/auth tokens and query strings.

## New APIs and verification

The updated README covers Web Vitals, tags/timestamps, identity/UTM filters,
minute buckets, annotation CRUD, all 16 website-scoped analytics endpoints and
the generic scoped resource API. These capabilities depend on the server version
and permissions; this package does not update an Umami server or database.

Run `scripts/test-umami-integration.sh` for 51 real integration cases per target.
It manages a disposable Umami/PostgreSQL stack, readiness, reports and cleanup.
The same suite gates publication on native .NET 8, 9 and 10 in GitHub Actions.
Live paid-Cloud, ClickHouse, old-server and browser-recording behavior is outside
this stack's coverage; see the integration guide for exact scope.
