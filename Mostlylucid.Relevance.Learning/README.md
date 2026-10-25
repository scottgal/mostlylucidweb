# Mostlylucid.Relevance.Learning

An in-process experiment/learning extension over actual Relevance candidates. Emails are the worked example,
not a special case in this library. A caller-owned `RelevanceEngine` supplies candidate conversion and ranking;
the learner owns its separate `IRelevanceStore`, bounded worker, detected context groups and assignment ledger.

The main surface is `RegisterAsync`, `ChooseAsync` and `SaveAsync`; `GetAsync` and `SnapshotAsync` inspect evidence.
All public contracts have XML documentation. The project is packable, not published to NuGet.

```csharp
await learning.RegisterAsync(variant);
var decision = await learning.ChooseAsync(new InterventionRequest(
    decisionId, sessionId, context, observedAt));
await learning.SaveAsync(new InterventionEvent(
    eventId, decision.Id, "outcome", at, "checkout", reward, "purchase"));
```

Contexts and descriptors use versioned ordered [0,1] axes. The default detector is bounded online nearest-centroid
grouping, not Leiden. Optional explicit audiences do not change control/treatment assignment semantics.
Variants have immutable IDs, exact candidate envelopes and descriptors. An eligible control is required.

Warm-up is uniform random assignment. Later choices use a bounded-support descriptor reward model with uniform
exploration and the actual logged propensity. Relevance rank supplies a tie-breaking/content-similarity opinion;
its RRF score is never interpreted as an effect probability. The small ridge model is a baseline, not a claim
of universal causal identification or calibrated probabilities. Evaluate policy changes against a proper control.

Observed endpoints, including no-view cases, update assignment-level reward statistics. Censored/pending feedback
does not become zero reward. Duplicate events and terminal outcomes are checked atomically. Positive outcomes
also update the recommender through a persistent idempotent outbox. A failed affinity write cannot erase an already
committed outcome. Both records have explicit retention/capacity and separate store lifetimes.

The host supplies verified delivery/application evidence, exposure coverage, endpoint definitions, bounded rewards,
eligibility rules and exact source/producer versions. The library does not send messages or execute interventions.

See [the runnable sample](../samples/RelevanceLearning) and [the demo email lab](../Mostlylucid.Relevance.Demo).
