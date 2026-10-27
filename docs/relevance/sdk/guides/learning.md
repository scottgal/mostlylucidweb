# Intervention learning

`Mostlylucid.Relevance.Learning` ships an implemented contextual learner over ordinary Relevance candidates. It detects context groups, retains exact intervention versions, chooses with logged exploration probabilities and learns from observed rewards. The host executes interventions and reports verified observations; the SDK does not send email or change operational controls itself.

Construct `InterventionLearning(definition, store, relevance, clock?, random?)`. It owns its supplied store and a separate bounded worker; `relevance`, clock/random and external converter/predictor resources remain caller-owned. Scope the experiment identity/store to the authorised tenant/domain. A recommendation space does not automatically authorise an experiment.

## Main API

```csharp
await learner.RegisterAsync(variant);
var assignment = await learner.ChooseAsync(request);
await learner.SaveAsync(observedOutcome);
var journey = await learner.GetAsync(assignment.Id);
var evidence = await learner.SnapshotAsync();
```

`InterventionVariant` retains ID, ordered feature array, exact candidate JSON and control flag. Candidate envelope `id` must match variant ID. Reusing an ID with different content/features fails. Include one control and at least one alternative; the recommender's mandatory eligibility must retain both.

`InterventionRequest` retains ID, assignment unit, observed coordinates/time, optional eligible IDs, randomised assignment flag and optional explicit group. Retry identical ID/content to recover the same assignment. One assignment per retained unit; use a new episode identity for a new decision. Group detection uses pre-intervention evidence. A full detector abstains for a distant new group.

`InterventionDecision` retains exact content, pre-treatment context/group, support, assignment/deadline, alternatives, policy and realised probability. Uniform warm-up precedes epsilon-greedy preference. Randomised requests force uniform assignment. The score, propensity, relevance rank and estimated reward have different meanings.

## Observation lifecycle

`InterventionEvent.Stage` accepts attempted, delivered, applied, viewed, clicked, failed, outcome, censored. Preserve actual evidence time/producer. `Link` identifies an exact occurrence. `After` retains measured post-context; `Measures` retains raw objective components. Only outcome requires reward, in [0,1]. Outcome before assignment or after deadline is rejected.

No observed view is not proof of unread content. Assignment-level observed outcomes still count when view evidence is missing. Censored or pending feedback does not become zero reward. A terminal outcome cannot be accepted twice under another event ID. Identical transport retries do not train twice; conflicting reuse fails. Later nonterminal telemetry may append to a closed record within retention.

The ledger update and reward statistics commit together. Positive outcomes feed the existing recommender through a stable-ID persistent outbox, so failure in affinity storage cannot erase or duplicate the authoritative outcome. RelevanceEngine remains independently owned and drained after the learner.

## Settings and defaults

| Property | Default |
|---|---|
| Id | empty; required experiment identity |
| Version | v1; representation/objective version |
| ContextAxes / InterventionAxes | empty; supply 1–16 distinct ordered names each |
| CandidateStream / ResponseStream / ResponseType | emails / responses / helpful |
| Placement | campaign |
| GroupRadius | 0.18 RMS context distance |
| MaxGroups | 16; at most 64 |
| MaxVariants | 32; 2–64 |
| MinOutcomes | 30 completed group outcomes before exploitation |
| Exploration | 0.2; greater than zero through 1 |
| RidgePenalty | 0.1; positive |
| DescriptorRadius | 0.4 RMS support radius |
| HorizonSeconds | 3600; assignment-anchored endpoint window |
| RetentionHours | 168; must cover horizon |
| MaxRecords | 100,000; at least 100 |

Context/action coordinates are finite [0,1] arrays in declared order. The small ridge model shares evidence between supported nearby descriptors. It is not a calibrated probability or universal causal estimator. Nearest-centroid grouping is not Leiden; explicit groups are optional.

`SnapshotAsync` returns groups, exact variant evidence and a bounded recent-journey view. `GetAsync` reads an older retained decision even after it leaves that recent view. Standard errors describe observed samples, not a general guarantee under adaptive assignment/censoring. Evaluate policies using proper controls and retained propensity.

Public requests/results serialize under AOT through `LearningJson.Metadata`, for example `RelevanceJson.Serialize(snapshot, LearningJson.Metadata)`. Private persistence records already use generated metadata; callers do not implement a serializer for them.
