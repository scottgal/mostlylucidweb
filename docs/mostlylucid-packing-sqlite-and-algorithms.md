# mostlylucid.packing SQLite provider and algorithm research

Use SQLite as the first persistent provider for defined spaces, allocations, interval blocks and projection revisions. Pair indexed retrieval with exact fit checks, then use ranking or batch optimisation according to the query. The proposed public calls are described in the [API guidance](mostlylucid-packing-api-guidance.md).

This is a researched implementation direction. The first [standalone implementation](https://github.com/scottgal/mostlylucid.packing) now provides SQLite capacity, transactional grouped holds, expiry, persistence and bounded allocation-block queries. The SQL shapes below were exercised locally. R-tree retrieval, bitset acceleration, the richer display projections and the Relevance bridge remain extensions.

## Choose an index by the question

| Question | First mechanism to evaluate | Exact check or qualification |
| --- | --- | --- |
| Next or previous stored block | B-tree over scope, resource and ordered start key | Working windows and requested duration still determine feasible offers |
| Blocks overlapping a viewport | Indexed interval query, with an optional R-tree candidate path | Exact half-open overlap and visibility rules |
| Next service start in a dense time grid | Capacity counters projected to bitsets, or indexed free runs | One consistent resource bundle for the whole appointment |
| Spatial candidates inside or near a region | R-tree bounding-box retrieval | Containment, collision, clearance and support rules |
| Nearest to a time | Ordered searches on both sides of the anchor | Named distance metric and deterministic tie-breaking |
| Nearest to user preferences | Exact vector scoring over a bounded feasible pool | Representation compatibility and evidence policy |
| Broad high-dimensional similarity retrieval | Optional vector index through the Relevance adapter | Explicit recall, filtering and candidate-coverage guarantees |
| Best compatible set of placements | An applicable assignment, packing or constraint solver | Independent verification of the whole selected plan |

SQLite can combine index searches with ordering and covering indexes. Inspect each important query with `EXPLAIN QUERY PLAN` during development. Its output is diagnostic and can change across SQLite versions. [Query planning](https://sqlite.org/queryplanner.html), [EXPLAIN QUERY PLAN](https://sqlite.org/eqp.html)

## Establish the ordinary interval path first

The first calendar projection can store integer resource IDs and absolute five-minute slot boundaries. A block occupies `[start_slot, end_slot)`. Application IDs and authorised space identity map to these internal keys. Store exact original instants separately when the profile supports finer boundaries.

```sql
CREATE TABLE blocks (
    id INTEGER PRIMARY KEY,
    resource_id INTEGER NOT NULL,
    start_slot INTEGER NOT NULL,
    end_slot INTEGER NOT NULL CHECK (end_slot > start_slot)
);

CREATE INDEX blocks_by_resource_start
    ON blocks(resource_id, start_slot, id, end_slot);

SELECT id, start_slot, end_slot
FROM blocks
WHERE resource_id = :resource
  AND start_slot < :to_slot
  AND end_slot > :from_slot
ORDER BY start_slot, id;
```

An index ordered by resource and start can also support forward and reverse block navigation. Keyset pagination uses the complete ordering key, including ID ties. A service-gap query subtracts the relevant occupied intervals from working windows, then checks duration and the other required resources.

Intervals starting before the viewport can still overlap it. Keep both overlap predicates. A start-time index can visit older starts to test their end, so compare this path with a range index when history, interval lengths or broad viewports make that expensive.

The production schema adds block kind, allocation identity, state, expiry and projection revision. A returned block list describes one consistent read snapshot. Compact records and run-length encoding can reduce payloads before any frontend rendering work.

## Evaluate R-tree retrieval for overlap-heavy queries

SQLite's R-tree supports spatial and temporal range retrieval. `rtree_i32` stores signed 32-bit integer bounds; the default `rtree` uses floating-point bounds and outward rounding. Validate index coordinates and preserve exact domain data for verification. Check module availability at provider startup. [SQLite R-tree](https://sqlite.org/rtree.html)

```sql
CREATE VIRTUAL TABLE block_bounds USING rtree_i32 (
    id,
    resource_min, resource_max,
    start_min, end_max
);

SELECT b.id, b.start_slot, b.end_slot
FROM block_bounds AS r
CROSS JOIN blocks AS b
WHERE b.id = r.id
  AND r.resource_min <= :resource
  AND r.resource_max >= :resource
  AND r.start_min < :to_slot
  AND r.end_max > :from_slot
  AND b.resource_id = :resource
  AND b.start_slot < :to_slot
  AND b.end_slot > :from_slot
ORDER BY b.start_slot, b.id;
```

This range-first comparison query deliberately fixes join order. Populate each block's resource bounds with the same resource ID. Maintain the index and source rows in the same transaction. A 3D profile uses coordinate bounds and performs exact geometry checks on returned candidates.

A local check on SQLite 3.54.0 created 200,000 blocks and matched 100 generated range queries against the ordinary interval query. Forward and reverse navigation used the covering B-tree. The unconstrained join planner selected the B-tree first and looked up R-tree rows by ID. The explicit range-first variant applied all four R-tree bounds, then fetched matching rows and sorted the result. This verifies the query shapes and identifies a tradeoff to measure, without establishing a latency advantage for either path.

## Derive service-start masks from capacity

For a fixed grid, project each resource's remaining units to a bitset for the demand being queried. Bit `i` is one when slot `i` has enough remaining capacity.

For a resource needed over `n` consecutive slots, feasible starts follow this relation.

```text
starts(n) = free
          AND (free >> 1)
          AND ...
          AND (free >> (n - 1))
```

Calculate the technician mask with technician reset time, the station mask with station reset time, and the client mask with service duration. Intersect those start masks for one eligible bundle. Union the resulting masks across eligible bundles. Compute the full-duration mask for each named technician before combining alternatives, preserving technician identity through the service.

Word boundaries require carry-aware shifts or tile overlap, and all tile boundaries must preserve valid cross-boundary starts. Capacity greater than one needs counters and a threshold appropriate to the requested units. Keep resource facts and exact allocations so that deleting or expiring one hold can update the correct counters.

This is a proposed acceleration path for dense calendar queries. Compare it with a sorted free-run sweep on sparse calendars. Check the derived masks against the existing interval-based verifier, including partial slots, reset times and window boundaries, before using it in the provider.

## Build display projections around the viewport

Maintain allocations and derived display blocks separately within the provider. A view query supplies authorised resource IDs, a bounded period or region, requested block kinds, detail level and a payload limit.

Use resource/time tiles for calendars and bounded spatial tiles for physical views. Fetch visible tiles and a small bounded neighbour window. A zoomed-out view receives counters or coarse runs. A detailed view receives exact blocks with stable IDs. Personalised service offers are a query over those facts and the user's restrictions.

A resource change updates intersecting tiles and the start ranges whose allocations could touch it. For an occupied change over `[s,e)` and a fixed footprint length `L`, potentially affected continuous start positions satisfy `s-L < start < e`. Align that range to the profile's grid and clip it to the horizon. Each resource's cleanup footprint contributes its own range.

Return tile revisions, an as-of instant and an expiry-aware delta cursor. Deltas use upserts and tombstones. An expired cursor or missing history causes a bounded viewport resnapshot. A hold in an unrelated region should leave the visible tile revisions unchanged.

Cache general capacity tiles independently of user preferences. Personalised result caches additionally bind customer evidence, restriction, ranking and representation versions. Bound cache size, query periods, resources, candidate counts and payloads. Report resolution so a coarse capacity summary cannot masquerade as a guaranteed service appointment.

## Keep acceptance transactional in SQLite

Persist command identity, the complete resource allocation bundle, hold expiry and the change record together. Use a short write transaction to re-evaluate current policy and cumulative capacity before accepting the hold. Capacity checks for pooled resources need the full overlapping allocation sweep, including the proposed bundle.

`BEGIN IMMEDIATE` can obtain the write transaction before the check. SQLite allows concurrent readers but has one writer, and write admission can return busy. Use bounded admission/retry and stable command IDs. WAL supports concurrent reads and writes on a shared host. [Transactions](https://sqlite.org/lang_transaction.html), [WAL](https://sqlite.org/wal.html)

Store fixed allocations and active holds once. Confirmation retains their usage, and cancellation or expiry releases it once. Expiry remains an exact authority check even if the display projection worker is behind. Replayed commands retain their original outcome and deadline.

## Connect relevance with explicit candidate coverage

The first exact path is indexed restriction filtering, resource-feasible candidate generation, then vector or feature ranking over that bounded pool. A top-K heap can retain the best K during an exact scoring pass without sorting every candidate. Report the pool bounds and representation versions.

For a large catalogue, a relevance index can retrieve promising service or technician templates before packing generates times. This is a useful proposed shortlist mode. Widen retrieval when feasible results are sparse, and report approximation whenever a bounded shortlist cannot establish the global feasible top-K.

The local Relevance library already supplies named vector representations, rank fusion, user evidence and exact constrained retrieval. Its current `RankAsync` accepts stored candidates and portable metadata filters. An explicit supplied-candidate scoring operation or eligible-ID contract is still needed for exact ranking of Packing's transient feasible set. The bridge should preserve attribution records only for actual recommendation flows. [Current Relevance contracts](../Mostlylucid.Relevance/Contracts.cs), [engine API](../Mostlylucid.Relevance/RelevanceEngine.cs)

The optional SQLite HNSW package currently requires one live owner per vector table and uses exact retrieval for constrained Relevance queries. Its graph connection and consistency contract must be respected by the bridge. Keep this integration in `mostlylucid.packing.relevance` and use the existing vector-assets package where appropriate. [Existing SQLite vector package](../Mostlylucid.Sqlite.Vector/README.md)

## Compare related systems

OR-Tools and Timefold already cover broad planning problems. The proposed contribution here is the C# dimensional-space API joining navigation, display projections, feasible search, personalised ranking and backend selection. These projects provide both reusable components and design ideas.

| System | Applicable strengths | Features to borrow or adapt |
| --- | --- | --- |
| OR-Tools | C# constraint, scheduling, flow and assignment solvers | Model capabilities, search limits, objective bounds and independently verified outcomes. [Official guide](https://developers.google.com/optimization) |
| Timefold | Constraint-based planning, score levels and changing plans | Named score breakdowns, pinned assignments and penalties for disturbing existing plans. [Scoring](https://docs.timefold.ai/timefold-solver/latest/constraints-and-score/overview), [replanning](https://docs.timefold.ai/timefold-solver/latest/responding-to-change/non-disruptive-replanning) |
| PackingSolver | Specialised geometric models and several algorithm families | Structural algorithm selection, cheap bounds, repeated-item reduction and anytime improvement. [Selection rules](https://fontanf.github.io/packingsolver/internals/algorithm_selection.html) |
| RectpackSharp | Small C# rectangle packer with tuning hints | Straightforward input/output and quality/runtime controls for a 2D profile. [Repository](https://github.com/ThomasMiz/RectpackSharp) |
| 3DContainerPacking | C# EB-AFIT box placements with rotations | A specialised 3D heuristic backend evaluated against bounded candidate enumeration. [Repository](https://github.com/davidmchapman/3DContainerPacking) |
| TimePeriodLibrary | Time ranges, gaps, intersections and calendar operations | Explicit period algebra and gap calculations in the calendar profile. [Repository](https://github.com/Giannoudis/TimePeriodLibrary) |
| iCal.NET | Recurrence, calendar interchange and time zones | Bounded recurrence expansion, exception dates and import/export through an optional calendar adapter. [Repository](https://github.com/ical-org/ical.net) |

## Route to algorithms that match the model

The following choices are proposed defaults to validate on representative fixtures. Backend compatibility must be checked against every required constraint and objective before selection.

| Problem shape | Candidate approach | Selection consideration |
| --- | --- | --- |
| Single request on sparse calendars | Indexed range retrieval and gap sweep | Reuse ordered working and occupied intervals |
| Many queries on a bounded dense grid | Capacity counters and bit-parallel start masks | Measure update cost, grid precision and memory as well as lookup speed |
| Simple worker-to-job assignment with linear costs | Linear assignment or min-cost flow | Applicable when all constraints have the required assignment/network structure. [C# flow example](https://developers.google.com/optimization/flow/assignment_min_cost_flow) |
| Rotas with interacting coverage, qualifications and rest rules | CP-SAT with interval/capacity constraints; local search as a later backend | Variable start times can avoid enumerating every time choice. [Scheduling example](https://developers.google.com/optimization/scheduling/job_shop) |
| Independent scalar bin capacities | First-fit decreasing or best-fit baseline; integer optimisation when a proof matters | Preserve independent weight, memory or other resource limits. [Bin-packing model](https://developers.google.com/optimization/pack/bin_packing) |
| 2D rectangles | MaxRects or Skyline heuristics; guillotine strategies for cutting restrictions | Select by layout quality, runtime, rotations and permitted cuts. [Algorithm implementations](https://github.com/juj/RectangleBinPack) |
| 3D boxes | EB-AFIT or a specialised geometric solver | Check weight, support and loading rules supported by the chosen backend. [C# implementation](https://github.com/davidmchapman/3DContainerPacking) |
| Irregular nesting | No-fit polygon methods with a placement/order search | Evaluate shape complexity and clearance. [SVGnest](https://github.com/Jack000/SVGnest) |
| Large changing plans with a feasible incumbent | Local neighbourhood or ruin-and-recreate search | Preserve pinned decisions and expose improvement limits. [Local search](https://docs.timefold.ai/timefold-solver/latest/optimization-algorithms/local-search), [move scoring](https://docs.timefold.ai/timefold-solver/latest/constraints-and-score/performance) |

PackingSolver demonstrates a portfolio driven by objective and instance structure. Borrow the staged approach of simplification, bounds, applicable specialised algorithms and incumbent improvement. For Packing, select first by query intent, supported rules, grid/shape structure, objective and requested guarantee. Return the selected strategy and its reasons with diagnostics. [PackingSolver algorithm selection](https://fontanf.github.io/packingsolver/internals/algorithm_selection.html)

The existing component cache already helps independent regions and adds overhead when shared resources connect the whole model. Use that measured connectivity information in strategy selection. Pinned decisions are hard fixed allocations. A stability preference can penalise changes to tentative assignments. Incremental scoring inside a connected component is a further optimisation beyond reusing unchanged components.

## Implement in this order

1. Stabilise the typed space query and outcome contracts, including period unions and cursor ordering.
2. Extract the package into its dedicated repository and configure verified artifact publishing through GitHub OIDC.
3. Add the SQLite source tables, transactional holds, versions and ordered block queries.
4. Add viewport block pages, tile revisions and bounded deltas, with cross-day and expiry checks.
5. Measure indexed gap searches against grid masks. Add R-tree retrieval where the measured query shape benefits.
6. Implement the Relevance feasible-pool scoring contract and the separate bridge assembly.
7. Add explicit strategy routing and compare specialised backends on the same model and verifier.

Acceptance measurements should include p50/p95 query and update latency, rows/candidates examined, payload size, managed/native memory and exact-search agreement or ANN recall. Use sparse and dense calendars, overlapping long intervals, multi-resource services, DST boundaries, cold-start users, pinned plans, adversarial constraints and incomplete search windows. Retain exact baselines and explain any reduced guarantee in the public result.
