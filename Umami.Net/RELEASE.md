Umami.Net 1.1.0 adds support for the current Umami 3.4 analytics and tracking APIs,
with a complete documentation refresh and a real-server integration gate.

## Added

- Current JSON collection responses and explicit per-visitor cache tokens.
- Web Vitals/performance collection, tags, historical timestamps and visitor/device overrides.
- Bearer API keys for self-hosted Umami and current Cloud API paths/authentication.
- All 16 website analytics endpoints, typed annotation CRUD/paging and scoped generic resources.
- Identity/UTM dimensions and filters, minute buckets and explicit API-version selection.

## Fixed

- Unbounded reauthentication, incorrect token-verification method and incomplete 2FA login handling.
- Event/identity data loss, visitor-agent/IP/query capture and current stats/active response parsing.
- Request/response disposal, background shutdown draining and unbounded queue memory.
- Automatic retries of non-idempotent writes, registration-wide logging changes and token/body logging.
- A single-framework override that prevented the default build from producing all target assemblies.

## Upgrade and docs

```sh
dotnet add package Umami.Net --version 1.1.0
```

Read the [upgrade guide](https://github.com/scottgal/mostlylucidweb/blob/umamiv1.1.0/Umami.Net/MIGRATION.md)
for API-version defaults, real user-agent handling, response ownership, retry
behavior, background backpressure and validation changes. Existing public entry
points remain available; newer server features require corresponding server support.

- [Usage and API examples](https://github.com/scottgal/mostlylucidweb/blob/umamiv1.1.0/Umami.Net/README.md)
- [Review findings and verification limits](https://github.com/scottgal/mostlylucidweb/blob/umamiv1.1.0/Umami.Net/REVIEW.md)
- [Integration coverage and local runner](https://github.com/scottgal/mostlylucidweb/blob/umamiv1.1.0/Umami.Net.Test/Integration/README.md)

The NuGet package includes the README and supplementary docs. The attached
`Umami.Net-docs.zip` also contains the integration guide and release notes.

## Validation

Native .NET 8, 9 and 10 builds and unit/contract tests, followed by **51 real
integration cases per target** against disposable Umami 3.4.0 and PostgreSQL 16.
The tests cover every analytics endpoint, tracking/identity overloads, actual
persisted values, filters/paging, resource lifecycles, permissions, revoked keys,
background delivery, cancellation and real socket failures. TRX reports are
available in the publishing workflow's artifacts. Package publication is gated
on these checks. No Umami production server or database is modified by this release.
