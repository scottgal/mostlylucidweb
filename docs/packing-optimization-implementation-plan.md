# Resource Vector Optimisation in C# Implementation Plan

Build a small reusable C# library that selects compatible placements under independent resource limits, then demonstrate it with server allocation, employee shifts and salon appointments. Keep domain rules readable and retain the existing booking authority for holds and confirmations. There is no UX in this phase.

The central abstraction is a candidate placement: one possible way to fulfil a request, its resource consumption and its preference features. A solver chooses a combination of candidates. Physical geometry remains a separate concern; weight and volume limits alone cannot prove that boxes fit.

The implementation is runnable in [samples/ResourcePacking](../samples/ResourcePacking/README.md). It covers the solver, server and calendar adapters, customer history, single-offer holds, offer template invalidation and selective component solves. Larger synthetic fixtures compare full and selective solves with matched proven objectives. A bounded 3D adapter generates placements at supplied anchors and expresses collision exclusions through the same solver. Specialised industrial packers, irregular nesting and interval-based performance comparisons remain extensions.

The standalone package and repository will be `mostlylucid.packing`, published through GitHub trusted publishing. The [public API guidance](mostlylucid-packing-api-guidance.md) defines dimensional spaces, navigation, period queries, relevance ranking and display blocks. The [SQLite and algorithm research](mostlylucid-packing-sqlite-and-algorithms.md) sets the first provider and algorithm-selection direction.

The initial standalone checkout is now at `../mostlylucid.packing`, with the private GitHub repository [scottgal/mostlylucid.packing](https://github.com/scottgal/mostlylucid.packing). It extracts the core, adds a SQLite provider and typed profiles, and exercises staged services and atomic friend bookings through project references and packed NuGet artifacts. Its release workflow uses OIDC after verifying package consumption. NuGet trust-policy configuration precedes the first release.

## Starting point

The existing samples already provide the pieces to connect:

- `samples/PackingCalendars`: elementary packing, independent CPU and memory limits, intervals and geometry checks.
- `samples/NailSalonScheduling`: staff rostering, eligibility, appointment resource bundles, preference scoring and simple customer history learning.
- `samples/StreamingAvailability`: versioned availability, a moving window, authoritative holds, expiry and confirmation.

Preserve these as standalone teaching examples. Add adapters and narrowly scoped extraction points rather than replacing Parts 1 to 3 with a framework. The current appointment planner combines eligibility, candidate generation and calendar checks; the current recommender keeps feature extraction private. Expose reusable pure helpers where needed, with existing checks protecting behaviour.

## Architecture and project boundaries

Use these proposed projects under `samples/ResourcePacking/`:

| Project | Responsibility |
| --- | --- |
| `ResourcePacking.Core` | Immutable problem model, validation, independent solution verification, greedy baseline and solver contract |
| `ResourcePacking.OrTools` | CP-SAT model construction and result translation; owns the native solver dependency |
| `ResourcePacking.Examples` | Console demonstrations and adapters for servers, shifts, salon appointments and streaming proposals |
| `ResourcePacking.Tests` | xUnit checks, exhaustive small-instance oracle and integration tests |

Target .NET 10 to match the existing samples. Keep OR-Tools out of the core and earlier articles' samples. Begin with these two solver implementations; introduce more extension points only when a second real use requires them.

## Milestone 1 Prove the solver integration

Before committing to the adapter, restore and run a tiny OR-Tools model locally on the development machine. Check the published package and native runtime assets for the intended deployment platforms, pin the selected version and record the commands. Do not assume a .NET API guarantees native support on every architecture.

The spike must exercise Boolean decisions, capacity inequalities, a small integer objective and a time limit. Verify result status and selected values. Check the supported interruption API before promising cancellation of native search; otherwise document the enforced time limit and bounded cancellation latency.

Acceptance: a clean restore/build/run succeeds locally, the dependency licence and runtime requirements are recorded, and a separate process can run the same smoke example. If native support fails, resolve that before building an application abstraction around it.

## Milestone 2 Define the shared resource model

Start with explicit records and a small solver contract. These are proposed shapes, not implemented APIs:

```csharp
public readonly record struct DimensionId(string Value);
public sealed record Capacity(DimensionId Dimension, long Available);
public sealed record Consumption(DimensionId Dimension, long Units);
public sealed record Candidate(
    string Id,
    string RequestId,
    IReadOnlyList<Consumption> Demands,
    IReadOnlyList<long> PreferenceFeatures);
```

The problem additionally contains requests, capacities, objective settings and snapshot identity. Requests specify whether fulfilment is optional or required. The model owns defensive copies; an `IReadOnlyList` alone does not make caller-owned arrays immutable.

Each resource dimension has one declared meaning and unit. Server dimensions might be `server:a:cpu` and `server:a:memory-mib`. Calendar dimensions represent a resource at an absolute five-minute UTC slot. Store only nonzero consumption entries, using a sparse representation rather than allocating a dense resource-by-candidate matrix.

Available capacity is the residual after existing bookings and active holds. Those allocations are counted once and remain fixed. Domain adapters reject unknown or blocked clients, unsupported services and unsuitable stations before emitting candidates. Skills are explicit eligibility rules, not approximate similarity scores.

Validate unique IDs, known request and dimension references, nonnegative capacities, positive demand units, feature-vector lengths and checked arithmetic. Aggregate duplicate consumption entries with overflow checking. Specify bounded integer scales for features and coefficients; reject non-finite values before any conversion from doubles. Resource feasibility never uses floating-point tolerances.

Acceptance: one validator and one independent verifier work for both server and calendar fixtures, and malformed input fails with useful diagnostics.

## Milestone 3 Implement selection and objectives

For every candidate create a Boolean decision `x`: select it or leave it out.

1. Each optional request selects at most one candidate; each required request selects exactly one.
2. For every dimension, total selected consumption must not exceed residual capacity.
3. Optimise the declared objective over feasible combinations.

For the introductory examples, maximise the number of fulfilled requests first, then maximise preference utility without reducing that count. Implement this as two bounded solves rather than an arbitrary giant weight. Only fix the primary objective as optimal when the first solve proves it; if it times out with a feasible incumbent, return that incumbent with its actual guarantee. Budget both passes within one total deadline. Keep the best verified incumbent if a later pass times out.

Provide separate objective profiles for different domains. Server consolidation minimises used servers after required workloads fit; it needs bin-usage decisions and linking constraints. That is a later extension, not the same objective as maximising accepted appointments. In the first server example, maximise admitted workloads under fixed server capacities.

Return selected candidates, unfulfilled requests, objective values, elapsed time, snapshot identity, candidate-generation completeness and solver outcome. Distinguish proven optimal, feasible, infeasible, no solution before the limit and invalid input. A timeout without a solution is not proof of infeasibility. An optimum over a truncated candidate pool is not a global optimum over all possible placements.

Implement a deterministic greedy baseline through the same contract. Independently verify every returned solution, including the baseline: selected IDs, per-request multiplicity, required requests, every capacity and recomputed objective values. A fallback is marked as a fallback and must satisfy required requests before being returned as a complete feasible solution.

Acceptance: exhaustive enumeration on small problems confirms the CP-SAT optimum; the verifier rejects deliberately corrupted plans.

## Milestone 4 Demonstrate the generalisation

Use three fixtures in increasing complexity:

1. **Servers:** a workload has alternative placements on machines; each consumes CPU and memory. Unused RAM cannot compensate for insufficient CPU.
2. **Employee shifts:** a shift request has candidate qualified employees. Each placement consumes the employee's time cells and daily-minute allowance. Candidate generation enforces time off and skills. Required uncovered shifts produce an infeasible result rather than silently publishing a partial roster.
3. **Appointments:** a customer request has alternative technician, station and start combinations. Consume technician time including technician reset, station capacity including station reset, and client time during service. Use the published roster as available supply.

Keep staffing and customer booking as two explicit stages initially. Optimising a draft roster does not make an employee available until that roster is published. Joint staffing and appointment optimisation can come later with conditional capacity constraints.

Show a counterexample where a flexible customer receives Anna under greedy ordering, preventing a second customer who can only use Anna from booking. The optimiser assigns the flexible customer to Cara and serves both. Print rejected alternatives and resource conflicts using factual explanations; do not claim these explanations are a formal minimal conflict proof.

Acceptance: all three adapters use the same selection engine, and the salon adapter preserves blocked-client, exclusion, accessibility, cleanup and back-to-back booking behaviour.

## Milestone 5 Connect recommendation features

Extract the existing time-fit, preferred-technician, continuity and quiet-station calculations into a reusable pure component. Preserve their existing meaning and ordering for the single-request example. Convert scores to documented bounded integers for the solver.

Keep resource consumption and preference features in distinct types. Learned customer history proposes requests and preference inputs; it does not weaken eligibility. A weighted utility is not a probability of acceptance. Start with transparent fixed weights and existing history learning; train a new ranker only when suitable outcome data exists.

Rank individual offers for one customer, but optimise combinations for a batch of requests. A shared resource couples those decisions, so picking each customer's top offer independently is insufficient. Automatic suggestions do not create holds before a customer engages.

Defer fragmentation penalties until they have an explicit model. The cost of leaving awkward gaps depends on the selected combination and cannot generally be represented by adding a fixed score to each candidate.

Acceptance: personalised preferences change choices among feasible plans; high preference scores never override hard requirements; future history cannot leak into profile generation.

## Milestone 6 Connect the moving window and holds

Create an immutable planning snapshot carrying a venue identity, resource and policy versions, the availability cursor and horizon. Solve outside the booking authority's lock. A proposed plan records the exact snapshot it used.

Index candidates by the resource ranges they touch. Availability changes invalidate affected candidates; a changed resource can also couple otherwise separate requests. Rebuild and solve the connected affected component, with explicit candidate, horizon and time budgets and a bounded wider rebuild fallback. Reuse prior decisions as hints where supported; this is a fresh bounded solve, not a claim that the native solver incrementally maintains the old search.

For one accepted offer, reconstruct its resource bundle from current trusted policy and ask `BookingLedger.TryHold`. Check current eligibility and policy versions through an application coordinator; the ledger alone checks capacity, not service rules. Coordinate policy publication and hold acceptance so a policy change cannot slip between validation and commit. A stale proposal can fail and trigger a bounded refresh. Keep existing holds and confirmed bookings fixed.

The first integration commits one customer choice at a time. A multi-request optimisation result is advisory and is not an atomic batch reservation. Do not implement a loop of holds and call it atomic. If all-or-nothing batch acceptance becomes a requirement, add a separate authoritative batch command with whole-bundle validation and idempotency.

Acceptance: two competing proposals cannot exceed capacity; expired holds free capacity once; stale policy cannot authorise an invalid appointment; a rejected commit causes a bounded retry without silently choosing a different appointment for the customer.

## Milestone 7 Measure before changing representations

Measure candidate generation, sparse model construction, solve time and independent verification separately. Report horizon, resources, requests, candidates, nonzero demands, objective quality and allocations. Compare against the greedy baseline on identical snapshots. Use fixed small fixtures for correctness and larger seeded fixtures for repeatable measurements; do not assert timing thresholds in ordinary unit tests.

Begin with five-minute capacity rows because they explain the vector formulation directly. If their size becomes the bottleneck, benchmark a scheduling adapter using OR-Tools interval variables, `AddNoOverlap` and `AddCumulative`. Preserve the shared request/result contract while allowing a richer domain model underneath. There is no promise that every problem will flatten efficiently into the same matrix.

Likewise, SIMD may accelerate some dense arithmetic, but sparse access, candidate growth and combinatorial search must be profiled first. A vector representation does not imply that nearest-neighbour search, a vector database or GPU execution is the right optimiser.

## Geometry extension after the scheduling slice

Keep physical coordinates and geometry constraints separate from capacity and preference vectors. Add a geometry adapter only after the scheduling path works:

- Rectangles: evaluate RectpackSharp or a dedicated rectangle formulation.
- Boxes: benchmark the C# EB-AFIT implementation against relevant shipping fixtures.
- Irregular shapes: use polygon operations such as Clipper2 inside a dedicated nesting strategy.

Bounded candidate placements can share selection constraints, including explicit pairwise collision exclusions. Candidate generation still has to find useful rotations and positions; support, stability, unloading order and irregular collisions remain domain rules. Do not advertise arbitrary-dimensional or irregular-shape packing from a resource-vector check alone.

## Verification and delivery order

Run the existing three sample check commands before and after extraction. New tests cover invalid models, mixed resource limits, multiplicity, mandatory requests, exhaustive optimality, score conversion, overflow, incomplete candidate sets, solver outcomes, deadlines and independent solution verification. Integration checks cover domain eligibility, snapshot consistency, stale proposals and concurrent hold acceptance. Test cancellation using the actual supported native behaviour rather than a mock claim of immediate interruption.

Deliver in this order:

1. Native solver spike and pinned dependency.
2. Core model, verifier, greedy baseline and exhaustive oracle.
3. CP-SAT selection and server demonstration.
4. Shift and salon adapters with the greedy counterexample.
5. Recommendation features and transparent objectives.
6. Streaming proposals and authoritative single-offer holds.
7. Measurements, then justified performance or geometry extensions.

Each slice builds and runs locally before the next one starts. Persistence, payments, distributed coordination, UX and packaging as a public NuGet library are later scope decisions.

The accompanying Part 4 article should introduce vectors with CPU and memory, explain selecting compatible candidates, demonstrate the greedy failure, then show the C# solver integration and reconnect it to the salon and streaming examples. Include a small resource table, a candidate-conflict diagram and the propose/hold sequence. Introduce matrix notation only after the concrete examples. Keep advanced interval modelling and geometry as linked extensions.

## Primary references

- [OR-Tools C# bin packing](https://developers.google.com/optimization/pack/bin_packing)
- [OR-Tools .NET CP-SAT model API](https://or-tools.github.io/docs/dotnet/classGoogle_1_1OrTools_1_1Sat_1_1CpModel.html)
- [CP-SAT statuses and integer modelling](https://developers.google.com/optimization/cp/cp_solver)
- [Solver search limits](https://developers.google.com/optimization/cp/cp_tasks)
- [RectpackSharp](https://github.com/ThomasMiz/RectpackSharp)
- [C# 3DContainerPacking](https://github.com/davidmchapman/3DContainerPacking)
- [Clipper2 successor repository](https://github.com/PolyClipping/Clipper2)
