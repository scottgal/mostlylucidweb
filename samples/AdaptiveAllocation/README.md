# Adapt allocations when the request, resources or evidence changes

A .NET 10 console consumer of the actual `mostlylucid.packing.ortools` and `Mostlylucid.Relevance` packages. It demonstrates three changes using the same finite selection core:

- A high chair changes the table footprint of a four-person party.
- A chef protects some specials for eligible customers, then releases that reserve.
- An LLM work item can run locally, online or through a deferred supplier. Learned task performance, preferences, capacity and context replay costs affect the proposed allocation.

All suppliers, prices, observed outcomes and cache records are synthetic. No LLM endpoint is called. The sample produces proposals; it does not amend a booking, reserve durable stock or dispatch work. The core library code is unchanged.

## Run from the article repository

By default, project references use `Mostlylucid.Relevance` in this repository and the sibling `../mostlylucid.packing` checkout. Override `PackingSourceRoot` when the latter lives elsewhere.

```bash
dotnet run --project samples/AdaptiveAllocation -c Release
dotnet test samples/AdaptiveAllocation/Tests/AdaptiveAllocation.Tests.csproj -c Release
```

In a restricted workspace, add `--artifacts-path /tmp/adaptive-allocation-build` to keep intermediate/output files outside the sibling checkout.

The fixture selects these allocations:

| Change | Proposal |
| --- | --- |
| Four people | A four-place table |
| One of those people now needs a high chair | A six-place table and one high chair; five place units |
| Chef protects one of three specials | At most two public orders use the special; an eligible customer can use the reserve |
| Chef releases the reserve | Higher-margin public orders can use all three specials |
| Fresh work with no history to replay | The cheaper online supplier |
| An 8,000-token conversation is retained locally | Stay local despite the operator's learned preference for online |
| That local context is evicted, or local execution slots run out | Reconsider online execution, including its replay tokens |
| More completed online failures are observed | A new fresh request favours local execution |

The native solver reports its actual status. A feasible plan can be returned without a proof of optimality. Search budgets do not bound the entire application call's wall-clock duration.

## What belongs where

`RestaurantAllocation` and `WorkRouting` are application adapters. They build `Request`, `Candidate` and `Consumption` input for the existing Packing core. The venue owns the high-chair footprint and the chef's reserve policy. The host owns permitted execution modes, target token estimates, capabilities and capacities.

Relevance owns the operator's behavioural affinity and ranks the supplied feasible pool through `RankCandidatesAsync`. The bounded rank-position bonus has an explicit maximum of two fixture utility points. Cold-start rankings contribute no preference bonus. RRF scores are never treated as success probabilities, and mandatory Relevance exclusions remove options.

`OutcomeMemory` is a small application-side estimator over completed task/supplier outcomes. It uses a smoothed empirical success estimate only after minimum support, reports that support and otherwise abstains. Missing or pending work isn't recorded as failure. Retry IDs deduplicate identical outcomes and reject conflicting ones. It is bounded, in memory, single-owner and not an inference SDK or a calibrated predictor. It deliberately stays outside Relevance until a second use establishes a reusable prediction contract. Supplier IDs must identify model/configuration versions whose outcomes are compatible; real deployment needs event time, retention and evaluation against later observations.

The solver first maximises fulfilled request count and then utility. The work example's utility is:

```text
rounded(task value points × empirical task/supplier success estimate)
  - new/replayed-input processing cost
  - retained-context read cost
  - output cost
  - explicit disruption penalty
  + bounded preference bonus
```

All terms are mapped into declared synthetic utility points. This is a policy demonstration, not a claim of monetary expected profit or calibrated quality. It doesn't implement a pure maximum-profit objective or an exploration policy for unknown suppliers.

## Context switching has an actual footprint

A `TokenQuote` uses the target supplier's tokenizer version. Each target can quote a different token count for the same material. A `ContextLease` must match work identity, context version and tokenizer, and remain unexpired at the snapshot time. The adapter treats it as a runtime fact supplied by the host. Merely naming the current supplier doesn't grant retained context.

```text
replay tokens        = quoted history - valid retained history
prefill work         = new input + replay tokens
full context window  = new input + all history + reserved output
```

Retained history can have a separate read charge. It still occupies the context window. Prefill, output and concurrent execution slots have independent shared limits; individually feasible requests must also fit together. A separate disruption penalty does not charge replay tokens a second time. Latency is a supplied estimate, not an execution deadline guarantee.

For real providers, adapters must establish whether history is actually retained, what must be transmitted again, and which quota/billing categories apply. A price-discounted remote cache is not automatically an active inference session. This fixture models prefill work; it doesn't claim that vendors measure all their rate limits in those units. Summarising a conversation changes its representation and needs a new version and evaluation, rather than silently pretending the old context was retained.

## Exercise the NuGet consumer

The sample has conditional package references, so it can run without source references to either library. Pack the existing projects into one local feed. Choose a fresh prerelease suffix when SDK source changes, since NuGet can otherwise reuse an older cached package with the same version:

```bash
ADAPTIVE_DEMO_VERSION=0.1.0-adaptive.20261112
dotnet pack ../mostlylucid.packing/src/Mostlylucid.Packing -c Release \
  -p:Version="$ADAPTIVE_DEMO_VERSION" -o /tmp/adaptive-feed
dotnet pack ../mostlylucid.packing/src/Mostlylucid.Packing.OrTools -c Release \
  -p:Version="$ADAPTIVE_DEMO_VERSION" -o /tmp/adaptive-feed
dotnet pack Mostlylucid.Relevance -c Release \
  -p:Version="$ADAPTIVE_DEMO_VERSION" -o /tmp/adaptive-feed
dotnet restore samples/AdaptiveAllocation/Tests/AdaptiveAllocation.Tests.csproj \
  -p:UsePackedAllocation=true -p:PackingPackageVersion="$ADAPTIVE_DEMO_VERSION" \
  -p:RelevancePackageVersion="$ADAPTIVE_DEMO_VERSION" \
  --source /tmp/adaptive-feed --source https://api.nuget.org/v3/index.json
dotnet test samples/AdaptiveAllocation/Tests/AdaptiveAllocation.Tests.csproj \
  -c Release -p:UsePackedAllocation=true -p:PackingPackageVersion="$ADAPTIVE_DEMO_VERSION" \
  -p:RelevancePackageVersion="$ADAPTIVE_DEMO_VERSION" --no-restore
dotnet run --project samples/AdaptiveAllocation -c Release -p:UsePackedAllocation=true \
  -p:PackingPackageVersion="$ADAPTIVE_DEMO_VERSION" \
  -p:RelevancePackageVersion="$ADAPTIVE_DEMO_VERSION" --no-restore
```

The default package-version properties match the local projects' preview versions; the overrides above exercise the new local artifacts explicitly. No package is published by these commands.

Tests cover resource footprint changes, protected/public inventory sharing, replay and cache-read charges, expired/evicted/incompatible context, target tokenizers, full context windows, shared quotas, execution modes, remote restrictions, latency estimates, observed outcome updates, Relevance cold starts and affinity, and idempotent bounded outcome ingestion.

## Next extensions

For Part 4, the next authority-level change is a replacement operation that evaluates an existing allocation's release and its replacement's acquisition atomically. The sample intentionally doesn't emulate this with two separate commands.

For Part 5, extend the restaurant adapter with characterised recipes, chef/service/customer context, projected ingredient stock and a held-out demand baseline. Today's counted-specials fixture is one finite stock constraint, not a recipe engine or a durable inventory ledger.

An optional language adapter can use schema-constrained output to propose typed request changes. The same application checks still apply. Free online provider adapters belong in the sample host, configured through endpoint/model/key settings; Packing and Relevance remain independent of a provider. Frozen responses and synthetic input should keep ordinary tests reproducible without an account. Live integrations, semantic quality evaluation, retained-context verification, pricing and dispatch remain further work.
