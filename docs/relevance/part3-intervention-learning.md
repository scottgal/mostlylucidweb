# Part 3: Change the outcome

Planned extension: `Mostlylucid.Relevance.Learning`. This document is a design for the next project, not a claim that causal intervention learning is implemented in the core preview.

## The series

| Part | Question | Demonstration |
|------|----------|---------------|
| 1. Understand the data | Which representation preserves relationships relevant to our decision? | Features, colours, embeddings and relevance |
| 2. Understand the behaviour | How does evidence accumulate, and what outcomes tend to follow? | Streaming statistics, recommendations and destination estimates |
| 3. Change the outcome | Which allowed action improves the outcome in this context? | Controlled interventions and adaptive action selection |

An observational probability such as “purchase is common after this state” is different from “showing product X instead of Y increases purchase probability”. Similarly, recovery after lowering a connection limit does not establish that lowering the limit caused recovery.

The core's current rank-head reliability rule is deliberately a heuristic. It is not the Part 3 intervention estimator.

## Keep the layers separate

Relevance supplies context, eligible candidates, evidence and baseline rankings. Learning chooses among allowed interventions and measures outcomes. The application owns execution or display; it reports what actually happened. A selection that was never shown or applied must not count as exposure.

Useful extension contracts:

- `IInterventionPolicy`: a probability distribution over the eligible actions, including an allowed control/no-op where appropriate.
- A durable decision/exposure ledger: context snapshot/version, eligible alternatives, chosen action or slate, assignment unit, policy version, selection probability, actual exposure/application time and source decision ID.
- An outcome definition: objective, measurement window, delayed/missing/censored feedback rules and bounded reward transformation.
- An evaluator/updater: learns from attributed exposures, preserves uncertainty and tests a proposed policy before adoption.

Keep the consumer API small: choose an action, then save exposure and outcome events. Use existing save/event conventions rather than a large imperative workflow API. Every operation needs stable IDs and restart-safe attribution. The decision ledger is owned by the learning extension, not reconstructed from today's mutable context.

## Ecommerce interventions

Start with one placement and a small eligible action set: show product X, show product Y, or show one of two images for the same product. Hold the rest of the page stable. Capture actual display, position, context, selection probability, and whether the defined purchase outcome occurred within the attribution window.

Products, images, price, placement and position are separate treatment dimensions. Do not change all of them at once and attribute the result to one image. Slates and position bias need a deliberate experiment design. Prefer an appropriate session/account assignment unit to avoid inconsistent treatment within a visit, and account for carryover.

## Operational interventions

Compare permitted actions such as reducing the local request limit, reducing a connection limit within configured bounds, adjusting consumer concurrency, or observing without a change. Define preconditions, scope, cooldown, limits and rollback in the action registry. The learning policy cannot make an ineligible action eligible.

Use an incident/episode as the assignment unit when requests share one system state. Allow a settling window, measure a fixed follow-up period, and record concurrent deployments and other interventions. Keep throughput and rejected-request costs alongside latency/error improvement: rejecting everything is not a successful fix.

The current demo records the pre-intervention metrics and current observations and makes a real, reversible local rate-limit change. Its synthetic recovery replay is an independent trace. Those comparisons are useful teaching evidence, but are not a causal experiment.

## First learning strategy

Start with a reproducible randomised control policy, followed by a small contextual bandit baseline. An epsilon-greedy policy is easy to inspect: most selections use the current preferred action and a configured fraction explore the eligible alternatives. Log the actual selection probability; do not derive it later from RRF scores.

Simple online reward statistics can update action preferences without a neural training pipeline. Predicted destination probability can be a context feature or prior; observed intervention outcomes still need to test whether the action helped. A better predictor is not automatically a better intervention policy.

The contextual-bandit framing and offline replay motivation are developed in [Li et al., 2010](https://arxiv.org/abs/1003.0146). Keep the first example focused on one-step decisions; multi-step intervention sequences require additional modelling of delayed consequences and policy effects.

## Evaluate what was actually supported

Randomised comparisons are the clean initial demonstration. For historical policy evaluation, propensity-based and doubly robust estimators are possible extensions when their assumptions and action support are satisfied. Unchosen actions with zero probability cannot be evaluated from those logs; small probabilities can create high-variance estimates. See [Dudík et al., 2011](https://arxiv.org/abs/1103.4601).

Report reward estimates, uncertainty, exposure counts, action coverage and the objective's component measures. Use held-out time periods or an independent policy evaluation stream, not the same observations both to select and to proclaim a winner. Define when an outcome is still pending instead of treating every delayed response as failure.

## Tests and experiments

1. A synthetic environment with known treatment effects, context-dependent winners, and a no-effect case.
2. Valid selection probabilities summing to one over the eligible set; zero probability for disallowed actions.
3. Exactly-once exposure/outcome attribution, delayed events, retries, missing feedback, restart and expiry.
4. Assignment-unit stability and a changed policy version halfway through an experiment.
5. A confounded recovery trace where naive before/after analysis would claim a false improvement.
6. Measurement of purchase rate/utility, policy regret on the known simulator, uncertainty, latency and cost.
7. Demo comparisons showing what changes when intervention learning is enabled, with a visible reset and explicit model/history version.

This keeps Part 3's central question testable: did choosing this action improve the outcome relative to an eligible alternative, for the context and measurement window we defined?
