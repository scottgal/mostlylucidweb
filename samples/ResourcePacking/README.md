# Resource vector packing in C#

The same solver selects server placements, employee shifts, salon appointments and finite 3D box placements. The console compares greedy assignment with CP-SAT, then sends one proposed appointment to the booking ledger as a hold.

```bash
dotnet run --project samples/ResourcePacking/Examples/ResourcePacking.Examples.csproj
dotnet run --project samples/ResourcePacking/Examples/ResourcePacking.Examples.csproj -- --measure
dotnet run --project samples/ResourcePacking/Examples/ResourcePacking.Examples.csproj -c Release -- --benchmark
dotnet test samples/ResourcePacking/Tests/ResourcePacking.Tests.csproj
```

`Core` contains the dependency-free model, solution verifier and greedy baseline. `OrTools` pins [Google.OrTools 9.15.6755](https://www.nuget.org/packages/Google.OrTools/9.15.6755), distributed under Apache 2.0. Its package dependencies include native assets for Linux x64 and Arm64, macOS x64 and Arm64, and Windows x64. The smoke model and the example were exercised locally on macOS Arm64 with .NET 10.

Each candidate belongs to one request. It lists the resource dimensions it consumes and the integer features used to rank it. Explicit candidate conflicts cover collisions between physical placements. The solver maximises the number of fulfilled requests and then the total utility at that count. A second search uses the remaining time from the first search's budget. The adapter observes cancellation between bounded native searches. Model construction and solution verification also take time, so the budget is a solver search limit rather than a hard wall-clock deadline for the entire application call.

For the salon, policy generates candidates and the moving availability view supplies capacity left after active holds and confirmed bookings. The plan is advisory. The coordinator rebuilds the accepted offer from current rules and the ledger checks capacity atomically when creating the hold. Each successful hold keeps its original expiry across command retries. An older proposal can still succeed if it remains valid and the ledger has capacity.

The example handles one salon day. `LiveSalon` caches eligibility-generated offer templates for the current request set. Changes to a request regenerate its templates. A client policy edit invalidates that client's templates. Holds, expiry and advancing time reuse the templates and refresh capacity from the moving view. Candidate IDs identify the request, technician, station and start, allowing an unchanged candidate to retain its identity as earlier times leave the window.

`IncrementalPackingSolver` builds connected components from all candidate alternatives, resource dimensions and explicit conflicts. An unchanged component reuses a proven optimal solution or an infeasibility proof. It constructs native solver input only for changed components. Feasible incumbents and unknown results are searched again. Components merge when new alternatives or conflicts connect them and split when those connections disappear. Signatures include required flags, resource limits, candidate IDs, consumption, utility and conflicts. Every combined plan is verified against the current full problem, with current snapshot metadata. Cache storage retains only proofs belonging to the current call's components.

A batch of recommendations is advisory and hold acceptance happens one choice at a time. The sample uses in-memory state, a fixed UTC grid and teaching fixtures. Template generation still enumerates the eligible offers for a changed request, and model construction visits the current bounded pool. The candidate limit caps the returned model after those offers have been generated.

`BoxPacking` generates the axis-aligned rotations that fit at supplied anchor points. It uses the Part 1 geometry code to identify collisions, passes those exclusions to the shared solver and checks the resulting physical layout again. Candidate generation has a hard count limit because pairwise collision checks grow quadratically. Results explicitly mark the candidate pool as incomplete because the solver searches the supplied anchors. Support, weight distribution and unloading constraints need additional domain rules for a shipping application.

`--measure` prints model size, per-run timings and reuse counts for the small salon example. `--benchmark` compares full and selective solves on synthetic salon areas with two technicians, eight client requests and eight candidate appointments per request over twelve five-minute slots. An unavailable technician moves between areas over five updates. From the second update onward, the restored area and the newly unavailable area both change. A third scenario shares station capacity across all areas, connecting the whole problem. Every timed update must reach matching proven counts and utility or the command fails. Input construction is timed separately. Full-solve timings include native model construction and verification. Selective timings also include partitioning, signatures and combined verification. Timing order alternates between updates. Managed allocation counts cover the calling thread and exclude the native solver's memory.

One local Release run on macOS Arm64 with .NET 10 produced these medians in milliseconds over five updates, after native runtime and cache warm-up.

| Synthetic fixture | Requests | Candidates | Demand terms | Input build | Full solve | Selective solve | Components solved and reused on final update |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| 8 independent areas | 64 | 512 | 6,656 | 6.38 | 34.98 | 10.00 | 2 solved, 6 reused |
| 32 independent areas | 256 | 2,048 | 26,624 | 15.64 | 199.84 | 21.03 | 2 solved, 30 reused |
| 8 areas sharing station capacity | 64 | 512 | 6,656 | 2.11 | 35.69 | 40.67 | 1 solved, 0 reused |

| Synthetic fixture | Capacity rows | Full managed allocation | Selective managed allocation |
| --- | ---: | ---: | ---: |
| 8 independent areas | 888 | 2.10 MiB | 2.45 MiB |
| 32 independent areas | 3,552 | 8.46 MiB | 6.65 MiB |
| 8 areas sharing station capacity | 811 | 2.03 MiB | 5.09 MiB |

The shared-station case exposes the overhead when the component spans the whole problem. Graph construction and signatures also allocate memory, so smaller independent fixtures can reduce search time while increasing managed allocation. These fixtures illustrate how connectivity affects reuse. Production latency and capacity need measurements with representative request distributions and update streams.

Read the [implementation plan](../../docs/packing-optimization-implementation-plan.md) and [Part 4: Adapting When the Plan Changes](../../Mostlylucid/Markdown/packing-algorithms-in-csharp-part-4.md).

The standalone `mostlylucid.packing` package will use the proposed [dimensional-space API](../../docs/mostlylucid-packing-api-guidance.md), with SQLite as the first storage provider and a separate Relevance bridge. The [provider and algorithm research](../../docs/mostlylucid-packing-sqlite-and-algorithms.md) compares suitable approaches.
