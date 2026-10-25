# SHOW ME WHAT YOU GOT (Part 4) : Mind Contol - The Subtle Art of Manipulation.

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

## Article lead: start with familiar audience experiments

The [working article draft](../../Mostlylucid/Markdown/show-me-what-you-got-part4-mind-control.md) opens with the title's joke and acknowledges that segmentation, ranking and learning are used to influence behaviour across commerce, advertising, entertainment and other systems. Keep the objective and the beneficiary explicit. The intro references [Meta's ranking explanation](https://ai.meta.com/tools/system-cards/instagram-feed-ranking/) and the FTC's [dark-patterns report](https://www.ftc.gov/reports/bringing-dark-patterns-light) without equating every experiment with a deceptive interface.

Begin the technical explanation with an audience, a control and a few variants. Familiar targeting identifies eligible sessions; random assignment supplies the initial comparison. Freeze the segment/context at assignment, rather than grouping participants by an outcome produced by the intervention. [Firebase's messaging experiments](https://firebase.google.com/docs/ab-testing/abtest-with-console) and [parameterised configuration experiments](https://firebase.google.com/docs/ab-testing/abtest-config) provide concrete examples of this familiar starting point.

Then introduce the generalisation: characterise the intervention as well as the entity. The useful input is an entity context plus the exact change and its descriptor. Shared parameters give a future learner something to compare across variants and contexts. Parametric experimentation and contextual policies already exist; the series demonstrates their integration with the streaming behavioural representation, replaceable feature producers and measured trajectories.

Proposed article progression:

1. Returning shoppers, fixed eligibility, a control and two price variants.
2. Exact variant identity plus a small, typed intervention description.
3. Allowed variant generation, with ordinary measurements and optional decision-model properties.
4. Assignment, delivery/application, observed engagement and downstream outcomes.
5. Pre/post trajectory change, forecast residuals and controlled outcome comparisons.
6. The same pattern for a rate-limit/concurrency change across incidents.
7. Update future choices from observed results, then evaluate generalisation across intervention parameters.

## Describe interventions in a transferable schema

Use an intervention family, versioned parameter schema, exact variant ID and content/configuration hash. Snapshot the proposed descriptor at assignment and the actual applied descriptor when execution differs. Record any mismatch; do not silently rewrite the decision history.

A compact descriptor mixes several kinds of properties:

| Property | Example | Producer |
|----------|---------|----------|
| Exact numeric setting | Price in GBP; requests per minute | Trusted application values |
| Relative change | Discount fraction; new limit divided by previous limit | Arithmetic converter |
| Timing and scope | Delay minutes, duration, placement, affected service | Application/configuration |
| Interpreted content property | Does the message explain delivery costs? | Optional decision model with a defined question |
| Provenance and availability | Schema, source hash, producer/rubric version, missing masks | Descriptor contract |

Translate useful parameters within a family, preserving original values and units. A five-percent price reduction can be represented consistently across products; it does not imply that five percent has an identical effect at every price. A rate-limit ratio supports comparisons across service sizes but does not make service contexts interchangeable. Do not create a universal vector that pretends discounts and connection limits have the same axes.

Decision models characterise properties that need interpretation. They do not supply observed delivery, readership, recovery or conversion facts. Keep yes/no proposition probabilities distinct from intensity scores, as in [Part 1.5](../../Mostlylucid/Markdown/show-me-what-you-got-part1-5-questions-as-coordinates.md). Deterministic values such as a configured price or limit remain direct measurements. Missing semantic readings retain availability; descriptions do not become invented zeroes.

A generator produces allowed variants, potentially using an LLM to propose copy. Characterise the generated content itself, rather than trusting requested labels such as "reassuring". Keep exact copy, source hash and producer version. The application validates price ranges, minimum margins, permissible settings and scope before selection. Generation is separate from policy choice, and neither is execution.

Start by changing one parameter within a family while holding others stable. Later designs can vary combinations, recording every dimension and preserving enough support to assess interactions. A descriptor lets a future effect model share evidence across variants; a similar vector is not proof of a similar treatment effect. New variants need deliberate exploration and uncertainty handling. Begin with exact-variant comparisons as a baseline, then test whether descriptor-based generalisation improves them.

## Observe the whole response chain

| Stage | Evidence to retain | Meaning |
|-------|--------------------|---------|
| Assigned | Chosen variant, alternatives and selection probability | Policy decision |
| Attempted | Send or handler invocation | An attempt occurred |
| Delivered/applied | Provider evidence or verified applied setting | Recorded delivery/application evidence |
| Exposure | View/read telemetry with source and reliability | Exposure evidence, not inferred from assignment |
| Engagement | Click, reply, interaction or acknowledgement | A separate observed response |
| Trajectory | Timed state, movement and normal-reference deviation | Subsequent behaviour |
| Endpoint | Purchase, cancellation, recovery, outage, or censored/pending | The defined result |

Support failed application, delivery failures, no observed exposure, pending feedback and unavailable telemetry. Avoid one all-purpose success boolean. An unobserved open is different from a known delivery failure and from a read followed by no purchase.

For email, "didn't read" should ordinarily be represented as **no observed open**, with a window and measurement coverage. [Apple's Mail Privacy Protection documentation](https://www.apple.com/legal/privacy/data/en/mail-privacy-protection/) explains that remote content may download without engagement, so pixel activity cannot establish a human open. A decision model reading the email cannot infer that its recipient read it. Retain raw evidence, source and uncertainty; clicks and replies are distinct evidence rather than proof of complete comprehension.

Keep an assignment-level analysis that includes every assigned unit. No-open and failed-delivery cases remain relevant to the policy's practical result, alongside observed purchases or other endpoints. Use engagement-stage summaries to diagnose the funnel; do not select only readers after randomisation and report that as the effect on the original audience. Pending or missing endpoint observation is still distinct from a confirmed non-purchase within a fully observed window.

## Measure deviation without teaching the model its own predictions

Record a pre-assignment context and its observation time. At declared follow-up times, capture the actual trajectory and endpoint. Report several different quantities rather than hiding them inside a single reward:

- Pre/post state change, direction, pace and acceleration, in compatible units.
- Departure from a qualified normal reference, retaining its version and support.
- Error against a pre-assignment forecast for the same target/window where one exists. Part 3 forecasts destinations; it does not supply an unimplemented future-vector forecast.
- Observed endpoint rates, component costs and utility under the assigned intervention versus the eligible control or alternative.

A change after an intervention and a deviation from an observational prediction are descriptive evidence. The controlled comparison estimates whether choosing that intervention helped under the experiment's assumptions. The predictor's own changed score is not a new outcome label. Keep the healthy-reference admission rule separate so an ongoing incident or treatment does not silently redefine normality.

For a rate-limit change, verify the applied value and scope, allow the specified settling period, and observe sustained recovery plus throughput and rejected work. Similar incident contexts may have different capacity or concurrent changes, so comparison and assignment happen at the appropriate service/episode level. The later policy can learn which allowed parameter changes tend to help those contexts, while retaining exploration and a control.

## Extend the trajectory visualiser

Retain the population of tracks from Part 3. Mark assignment and actual application/exposure separately, annotate the intervention's parameters, and show subsequent observed movement. Compare treated and control populations over a common declared window, with sample counts and pending/unknown stages visible. Use stage indicators for no observed email engagement and failed operational changes. Keep illustrative geometry, actual engine forecasts and measured experimental results labelled distinctly; do not animate a convenient recovery and call it a learned effect.

## Keep the layers separate

Relevance supplies context, eligible candidates, evidence and baseline rankings. Learning chooses among allowed interventions and measures outcomes. The application owns execution or display; it reports what actually happened. A selection that was never shown or applied must not count as exposure.

Useful extension contracts:

- `IInterventionPolicy`: a probability distribution over the eligible actions, including an allowed control/no-op where appropriate.
- A durable decision/exposure ledger: context snapshot/version, eligible alternatives, exact variant ID and parameter/descriptor snapshot, assignment unit, policy version, selection probability, stage events with evidence, actual exposure/application time and source decision ID.
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
9. Exact variant replay, parameter units and schema versions, generated variants outside allowed ranges, and proposed/applied descriptor mismatches.
10. No observed email open versus unavailable telemetry versus known failed delivery; primary results retain all assigned units.
11. New descriptor combinations with no outcome support abstain or explore explicitly; shared descriptors cannot manufacture observed success.
12. An action lowers latency by rejecting work: the recorded objective detects the throughput/rejection cost instead of declaring recovery from latency alone.

This keeps Part 4's central question testable: did choosing this action improve the outcome relative to an eligible alternative, for the context and measurement window we defined?


## Catalogue and marketing feedback

Use observed journeys to propose tests of missing product information, clearer descriptions, alternate copy/images, prices, special offers and appropriately timed emails. Preserve the distinction between a missing feature and evidence that the feature caused a bad outcome. An intervention changes one defined aspect, logs actual exposure and compares the response with a control.

A high purchase forecast does not imply discount responsiveness, and low propensity does not imply high uplift. Include costs, margin, unsubscribe/complaint rates, consent and contact eligibility in the relevant marketing objective. Pricing experiments need explicit assignment units and protection against concurrent offers or spillover. The outcome ledger should support later training of a treatment-aware model.

This is the direct DiSE connection: choose a bounded directed change, observe the response and update preferences from measured results. Start with transparent assignment and counts; add a trained model only when it improves the measured decision.

## Timestamped context for interventions

Part 3 now supplies actual centroid position, velocity, acceleration, elapsed episode time and qualified normal-reference evidence. Snapshot their representation/version and observation timestamp in the decision ledger, rather than reconstructing them after the response. A forecast's horizon starts at its observation. The primary assignment-level experiment uses a declared common follow-up anchor, such as assignment or scheduled send time, including controls and failed/unobserved exposures. Secondary response windows may start at actual application or observed exposure when that is the specified question; retain the stage and avoid silently switching populations. These clocks must not be pooled as if they defined the same target. A return towards the normal centroid is evidence to measure alongside sustained recovery, throughput and intervention costs, not proof that the action caused recovery.
