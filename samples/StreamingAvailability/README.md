# Streaming availability

Runnable logic for [Packing Algorithms in C# Part 3](../../Mostlylucid/Markdown/packing-algorithms-in-csharp-part-3.md). Targets .NET 10 with no additional packages. No UX is implemented.

```bash
dotnet run --project samples/StreamingAvailability/StreamingAvailability.csproj
dotnet run --project samples/StreamingAvailability/StreamingAvailability.csproj -- --check
```

`BookingLedger` serialises one venue's capacity commands, records idempotent results, owns expiring holds and confirmed reservations, and appends an ordered in-memory change journal. `RollingAvailability` maintains a fixed-size ring per resource, applies event deltas and hydrates only the entering tail when time advances. It catches up retained cells and snapshots the tail at one source version. Duplicate events are ignored; missing sequences are rejected for catch-up or rebuild. The source can accept reservations beyond the read window, which become visible when their time enters it.

`AvailabilityWorker` uses a bounded channel as a wake-up signal and reads changes from the journal. Coalescing a signal does not drop journal events. A periodic timer advances time and expires holds during quiet periods. Use `TimeProvider.System` for a hosted worker; `ManualClock` controls logical time in the console and command tests, without pretending to implement virtual timers.

All appointment intervals must align to the five-minute UTC grid. Hold deadlines are exact instants and are checked under the command lock before confirmation. A hold uses the full resource bundle; confirmation retains its usage; cancellation or expiry releases it once. `OutsideWindow` means the cache cannot answer, rather than sold out.

This is a single-process, in-memory authority and projection. The lock does not coordinate multiple server instances, and a process restart loses the ledger and journal. Snapshot rebuild tests recover the projection while the authority remains present. Production needs durable booking state, command deduplication and an outbox written transactionally, plus consumer checkpoints and recovery. Persist/expire old history separately: the read window is bounded, but the demo retains its full source journal and command results.

The capacity layer expects trusted resource bundles from the application's eligibility checks. Customer restrictions, service capabilities and policy still belong to the Part 2 booking service. The sample does not implement payments, amendments, hold renewal or a persistent event broker. Reducing resource capacity below active holds or bookings is rejected for explicit resolution.

The checks cover expiry boundaries, retries, duplicate and missing events, 32 competing commands, quiet-period expiry, multi-resource accounting, roster edits, rollover/backlog consistency, bookings crossing the horizon, long clock jumps, snapshot rebuilds, asynchronous consumption and 500 generated update steps compared with an independent reservation-record recount.
