# Part 4: Experiment and change the outcome

Planned extension: `Mostlylucid.Relevance.Learning`. This document is a design for the next project, not a claim that causal intervention learning is implemented in the core preview.

## The series

| Part | Question | Demonstration |
|------|----------|---------------|
| 1. Understand the data | Which representation preserves relationships relevant to our decision? | Features, colours, embeddings and relevance |
| 2. Recommend | Which eligible candidates fit the current characterisation? | Streaming summaries, retrieval and rank fusion |
| 3. Predict | Where does this observed journey tend to end? | Fresh-model outcome learning and held-out forecasts |
| 4. Experiment | Which allowed action improves the outcome in this context? | Controlled interventions and adaptive action selection |

An observational probability such as “purchase is common after this state” is different from “showing product X instead of Y increases purchase probability”. Similarly, recovery after lowering a connection limit does not establish that lowering the limit caused recovery.

The core's current rank-head reliability rule is deliberately a heuristic. It is not the Part 4 intervention estimator.

## Keep the layers separate

Relevance supplies context, eligible candidates, evidence and baseline rankings. Learning chooses among allowed interventions and measures outcomes. The application owns execution or display; it reports what actually happened. A selection that was never shown or applied must not count as exposure.

Useful extension contracts:

- `IInterventionPolicy`: a probability distribution over the eligible actions, including an allowed control/no-op where appropriate.
- A durable decision/exposure ledger: context snapshot/version, eligible alternatives, chosen action or slate, assignment unit, policy version, selection probability, actual exposure/application time and source decision ID.
- An outcome definition: objective, measurement window, delayed/missing/censored feedback rules and bounded reward transformation.
- An evaluator/updater: learns from attributed exposures, preserves uncertainty and tests a proposed policy before adoption.

Keep the consumer API small: choose an action, then save exposure and outcome events. Use existing save/event conventions rather than a large imperative workflow API. Every operation needs stable IDs and restart-safe attribution. The decision ledger is owned by the learning extension, not reconstructed from today's mutable context.

## Define what to optimise

Destination probability and destination desirability are separate contracts. The application supplies a versioned objective: destination utilities, measurement window, costs, and constraints. Changing the objective must not change the predictor's empirical transition probabilities. Keep the raw outcome and objective version so recorded results can be explained and, where the necessary components were retained, rescored.

For a simple objective, expected utility is the sum of destination probability multiplied by destination utility. The Part 3 illustration uses purchase 1, cancel -0.5 and leave 0. These are preference units, not probabilities. Unresolved probability remains unknown value; do not silently assign zero or renormalise away missing mass. Sparse or incomplete forecasts need explicit uncertainty handling before policy selection.

Intervention selection needs action-conditional outcome estimates: expected utility under an eligible action, less its costs, compared with the control. The observational forecast is a baseline/context feature; it cannot supply the unmeasured treatment effect. Evaluate the policy on observed rewards under the stated objective, not on whether its own predictions increased.

Start with an explicit fixed objective and a small action set. Retail might optimise net contribution after discounts, contact costs and returns, with cancellation/complaint constraints. Operations might optimise sustained recovery within a fixed window, with minimum throughput and rejected-request limits. Keep hard constraints in eligibility rather than allowing a large reward to purchase permission to violate them. State which costs are already included in utility so they are not subtracted twice.

If an updater requires bounded rewards, define and version the mapping from raw utility to that range; signed utility must not be passed directly to the core's existing [0,1] feedback contract. Report raw component measures alongside the scalar reward. Objective changes create a new evaluation version; do not pool incompatible reward statistics.

## Ecommerce interventions

Start with one placement and a small eligible action set: show product X, show product Y, or show one of two images for the same product. Hold the rest of the page stable. Capture actual display, position, context, selection probability, and whether the defined purchase outcome occurred within the attribution window.

Products, images, price, placement and position are separate treatment dimensions. Do not change all of them at once and attribute the result to one image. Slates and position bias need a deliberate experiment design. Prefer an appropriate session/account assignment unit to avoid inconsistent treatment within a visit, and account for carryover.

Include prevention scenarios: a basket-abandonment trajectory can trigger an experiment with clearer delivery costs or product comparisons; a payment-cancellation trajectory can trigger clearer payment guidance or an alternative eligible payment method. Log the warning context before assignment. Define abandonment by an explicit window and payment cancellation by a suitable event; neither is inferred solely from a pause. The existing fixture's generic `cancel` is not yet a payment-specific outcome. Measure profitable completion and costs as well as the unwanted endpoint.

## Operational interventions

Compare permitted actions such as reducing the local request limit, reducing a connection limit within configured bounds, adjusting consumer concurrency, or observing without a change. Define preconditions, scope, cooldown, limits and rollback in the action registry. The learning policy cannot make an ineligible action eligible.

A concrete prevention scenario starts with increasing latency, error responses and queue growth. Predict a defined sustained service-level breach or outage, then compare an eligible concurrency/connection-limit change with the alternative. Record sustained recovery, throughput and rejected work over the same window; a reduction in the warning metric alone is not sufficient evidence of success.

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
8. Two objectives applied to the same destination forecast leave probabilities unchanged but can prefer different actions; intervention costs, hard constraints, unresolved mass and reward-range conversion remain explicit.

This keeps Part 4's central question testable: did choosing this action improve the outcome relative to an eligible alternative, for the context and measurement window we defined?


## Catalogue and marketing feedback

Use observed journeys to propose tests of missing product information, clearer descriptions, alternate copy/images, prices, special offers and appropriately timed emails. Preserve the distinction between a missing feature and evidence that the feature caused a bad outcome. An intervention changes one defined aspect, logs actual exposure and compares the response with a control.

A high purchase forecast does not imply discount responsiveness, and low propensity does not imply high uplift. Include costs, margin, unsubscribe/complaint rates, consent and contact eligibility in the relevant marketing objective. Pricing experiments need explicit assignment units and protection against concurrent offers or spillover. The outcome ledger should support later training of a treatment-aware model.

This is the direct DiSE connection: choose a bounded directed change, observe the response and update preferences from measured results. Start with transparent assignment and counts; add a trained model only when it improves the measured decision.
