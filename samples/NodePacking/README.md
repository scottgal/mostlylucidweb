# JavaScript ↔ C# interop with one booking authority

The main example loads the C# packing engine into an existing JavaScript app. JS reads its availability API, C# calls back into JS for names, and the existing JS API atomically commits the accepted proposal. The reverse example embeds Node inside .NET and calls the same API, including Promises and exceptions.

Requires .NET 10, Node 22 and a sibling `mostlylucid.packing` checkout. Executed on macOS ARM64. The JS API is an executable in-memory fixture to replace with your application's repository/service, not a supplied production database adapter.

## Run the application flow in both directions

```shell
npm ci
npm run build:app
npm run app
npm run reverse
npm run test:interop
```

`npm run app` shows JS → C# → JS callbacks for names, an API-owned booking, an identical command retry and a stale proposal rejected by the API. `npm run reverse` starts with C#, awaits the JS snapshot API, uses a JS name callback and commits through JS. Neither path opens a C# SQLite store.

`AppScheduler` adapts a per-call snapshot to the packing library's ICalendarStore interface. The adapter is read-only and its write methods throw. It is temporary search input, not a second booking database. The snapshot's primitive columns represent resource/grid cells. The fixture supplies a fixed catalog; an actual adapter must map catalog policy, availability, commitments, holds and client resources from the application's model.

The JS fixture's revision check, capacity validation, write and command-cache update run without an intervening await on one event loop. That is only sufficient for this one-process fixture. A real multi-process store must implement the same operation atomically, including its idempotency record. Browser input must not supply the authoritative resource demands. `createSchedulingFlow` retains those on the server, scoped to one workflow.

For another packing source location:

```shell
dotnet build EmbeddedNode -c Release -p:PackingSourceRoot=/absolute/path/to/mostlylucid.packing
```

The API uses a synchronous getName callback over preloaded JS data. Load async name data before entering C#, or design an async delegate and Task-returning operation. An async function cannot satisfy Func<string,string>. The sample tests callback errors and runtime recovery.

## Files to follow

| File/project | Role |
| --- | --- |
| `existing-calendar-api.cjs` | JS-owned read, lookup and atomic-write API fixture |
| `AppScheduler` | Exported C# engine and read-only snapshot adapter |
| `auto-scheduling.mjs` | Native module load, name callback, result snapshot and server-held proposals |
| `existing-app.mjs` | Main JS-hosted walkthrough |
| `EmbeddedNode/ExistingCalendarApi.cs` | JSReference, Promise conversion and typed native-value copies |
| `EmbeddedNode/Program.cs` | .NET-hosted walkthrough and exception checks |
| `format-booking.cjs` | Shared synchronous/async JavaScript formatter |

The article is at `Mostlylucid/Markdown/usingmicrosoftjavaScriptnodeapi.md`.

## Tests and declarations

```shell
npm run build
npm test
npm run typecheck
npm run test:interop
```

`build` also builds the older SQLite-backed fixture used for the benchmark and `booking`/`demo` scripts. Tests cover one-authority state changes, JS-owned names, server-held proposals, retries, stale revisions, malformed snapshots, sync callback contracts, cross-language errors, and the older fixture's stage-specific reservations.

Generated declarations describe requests, results and callbacks. The preview generator emits an unresolved ValueType base for structs, so the consumer check uses skipLibCheck for declaration internals. Consumer fields and arguments remain checked; this does not certify the entire generated declaration output. The nested result struct/appointment-array shape failed generation in 0.9.36, so result classes are exported instead. Reading their properties invokes managed getters; the JS wrapper snapshots them once without JSON text.

## Native AOT npm proof

```shell
dotnet publish NativeAddon -c Release -r osx-arm64
node NativeAddon/test.cjs
```

This builds a small name-provider callback helper, publishes a .node binary and packs an npm tarball. The local binary and a separately installed tarball were tested on macOS ARM64. It is not a Native AOT build of the whole scheduler. The package is a private demo and was not published to a registry. See `NativeAddon/README.md`.

## Browser WebAssembly recipe

`WasmSketch` contains overlay files and template/build instructions using System.Runtime.InteropServices.JavaScript. It is separate from Node-API. No wasm-tools workload is installed in the test environment; the sketch has not been compiled or run. The full packing core's trimming/AOT and browser compatibility still need validation.

## Retained interop/gRPC benchmark

```shell
npm run build:bench
npm run bench
npm run bench:summary
```

This uses the original standalone `PackingBridge` fixture and its in-memory SQLite test store, not the main application's API or name callbacks. It measures typed objects plus a full JS property snapshot, JSON interop, and gRPC carrying the JSON response. All modes invoke the same search with identical inputs/state. Results are consumed and validated; candidate/assignment equality is checked across modes.

Three trials rotate the mode order. Each fresh client performs 200 warmups and 1,500 measured calls per operation, concurrency one. Configure BENCH_TRIALS and BENCH_ITERATIONS to change that. The gRPC client reuses plaintext HTTP/2 on loopback with a per-call deadline. The runner starts/stops its own sidecar. It is not a fully protobuf-shaped API or a concurrent production service.

RSS snapshots follow the first valid search and the measured workloads, with natural collection. gRPC totals add the Node client and .NET sidecar; summed RSS can double-count shared pages. These are not peak/unique-memory or allocation-per-call measurements. A 10 ms timer during a 100-search burst illustrates responsiveness. The .NET hosts use workstation GC. Client-ready timings exclude starting the sidecar and are not full cold-start timings.

`bench/results.json` records the original measured environment, individual trials, methodology and assembly hashes. Re-running replaces it. Those numbers do not measure the new app API, callbacks, commit transaction, Native AOT addon or browser code. The recorded typed fixture was slightly slower than JSON at p50 once all properties were consumed.
