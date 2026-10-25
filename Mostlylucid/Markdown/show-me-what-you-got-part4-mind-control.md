# SHOW ME WHAT YOU GOT (Part 4) : Mind Contol - The Subtle Art of Manipulation.

The “mind control” in the title is a joke about a familiar ambition: put the right thing in front of someone at the right moment and change what they do next. Most of us have ignored enough perfectly timed marketing emails to know how unreliable that ambition can be.

But segmentation and learning really are used to try to manipulate many aspects of modern life: what we buy, which adverts reach us, what we watch next, which messages get our attention and whether we keep a subscription. Choosing what to show, when to show it and what to measure afterwards is a way of trying to influence behaviour.

These systems can help someone find a suitable product or understand a choice. They can also serve an objective that conflicts with the person's interests. Meta's [feed-ranking explanation](https://ai.meta.com/tools/system-cards/instagram-feed-ranking/) describes ranking around predicted interactions. The FTC's [Bringing Dark Patterns to Light](https://www.ftc.gov/reports/bringing-dark-patterns-light) documents examples where interfaces are designed to steer or obstruct consumer choices. Calling an optimisation target “engagement” or “conversion” leaves us with the practical question: whose result are we improving?

Our examples will keep that objective visible. A retailer might want profitable purchases without unwanted contact. A service operator might want recovery without rejecting all the useful traffic. A higher number on one dashboard is not enough to describe either result.

In [Part 3](/blog/show-me-what-you-got-part3-predict), we characterised a journey and estimated where it was heading. Now we want to introduce a change, observe the response and use that experience when choosing the next change. Before we get to adaptive intervention models, there is a familiar place to begin: audience segmentation and experiments.

<datetime class="hidden">2026-10-25T08:00</datetime>
<!-- category -- AI,Architecture,Machine Learning,Recommendations,Behavioural Inference,C#,Experimentation -->

[TOC]

# Give the Asteroid a Nudge

Imagine the user journeys from [Part 3](/blog/show-me-what-you-got-part3-predict) as asteroid tracks. We can observe where each one is, which direction it is travelling and how quickly that direction is changing. From comparable histories, we can estimate where it is likely to end up.

Now we want to give one of those journeys a nudge.

An email, a clearer explanation, a different offer or a timely reminder is an attempted intervention. The interesting question is whether the subsequent trajectory deviates from similar journeys **without that particular change**. An intervention that looks persuasive but produces no useful change has not achieved the objective.

This changes how we think about the email. It is not merely content to classify, or a document to rank. It is a proposed change to an evolving situation. Its structure and meaning describe the kind of nudge we attempted; the recipient's observed behaviour describes the response.

The comparison needs more than a before-and-after picture. Two shoppers might already have been heading towards different destinations. One might have purchased without the email. A service might have recovered without changing its rate limit. We need comparable starting conditions and a reference for what happens without the change.

For a journey visualisation, that means keeping the neighbouring control tracks visible. Mark the assignment and actual exposure separately, then follow the observed paths. The reference is a population of comparable untreated trajectories, not a single line we invented by extending the current velocity.

> **The nudge is a hypothesis.** The intervention descriptor is not a force vector that we can add to a shopper's position. Email characteristics and behavioural state belong to different spaces. Observed responses teach us the relationship between them.

This chapter follows that relationship through a working email example: characterise the proposed change, apply it under a recorded experiment, observe the response, then learn which changes tend to help which detected patterns.

The series is building up ML concepts from ordinary observations and small pieces of code. Part 1 established representations and similarity. Part 1.5 showed how classifiers can produce useful coordinates. Part 2 used those representations to rank candidates. Part 3 added time, trajectories and outcome prediction. Here we close the loop: choose an action, observe its result and use the evidence to improve later choices.

That brings us to **online learning, regression, contextual bandits and causal inference**. Those names describe different jobs in the system. We will introduce each where we need it, using the email experiment to explain what it buys us. The goal is to learn which permitted change helps in a particular situation, rather than merely predict who was likely to purchase anyway.

# Detect the Context Before Choosing the Push

Audience experimentation gives us a familiar starting point. Find people with relevant behaviour, compare variants with a control, then measure the result. Our difference is how the audience emerges.

We do not have to begin with a rule called “price-sensitive customer”. We observe product views, comparisons, price-filter changes and progress through the journey. Repeated patterns in those observations can reveal groups; a live session can then be compared with the discovered patterns.

In the example, the context has two deliberately small coordinates: comparison activity relative to views, and price narrowing. The learner receives the measured numbers. The simulator's hidden response type and any human-friendly audience name are excluded.

One detected pattern eventually has a mean near `[0.753, 0.697]`; another near `[0.149, 0.102]`. We can describe the first as more comparison-heavy and price-narrowing after looking at that evidence. The description does not create the group.

The current detector is bounded online nearest-centroid grouping. An observation close enough to an existing reference updates it; a sufficiently different observation starts another reference, within the configured limit. Feature scales, radius and observation order matter. This is a simple inspectable baseline, rather than an undisclosed Leiden implementation. [Part 1](/blog/show-me-what-you-got-streaming-recommendations#emergent-recommendation-discover-the-patterns) explains the richer community-discovery route.

> **ML aside: unsupervised versus supervised learning.** Grouping asks which observed behaviours resemble one another without requiring a correct audience label for each visit. That is the unsupervised part. Later, an observed reward supplies a target for predicting response: that is supervised learning. We can discover a group before we know which intervention helps it. Finding structure and learning what to do with that structure are separate tasks.

A segment is also not compulsory. A later policy could use the continuous position and trajectory directly. Explicit audience definitions remain available when useful. Permission to contact someone, minimum margins and allowed interventions are separate eligibility rules, not discoveries about their behaviour.

The experimental assignment comes afterwards. Within eligible comparable contexts, some visits receive the change and others receive the control. Record the position, movement, grouping evidence and representation version at assignment. A person's later behaviour must not be used to rewrite which starting group they belonged to.

That last point matters when we visualise the tracks. Moving away from a group after receiving a message might be the response we want to study. Reclassifying that person and then comparing the new groups would mix the intervention's consequences into our explanation of its starting conditions.

# What Kind of Nudge Is This Email?

Consider two emails about the same pair of walking boots. Both use similar words and link to the same product. One puts a button near the beginning, repeats the destination later, explains delivery and arrives after a short delay. Another presents the explanation first, offers a different price and arrives later.

A whole-message semantic embedding might put them close together. That can be useful: they concern the same product and purpose. It does not necessarily preserve the distinctions needed to understand their effects.

For this experiment, we want to describe the message along properties that can vary, be measured and be related to a response. We therefore keep an inspectable description of the email as well as its vectors:

| Aspect | Question it lets us ask |
|--------|------------------------|
| Subject and preheader | What appeared before the main body was read? |
| Body structure | How much explanation preceded the action? |
| CTA and other links | Which action was presented, where, and how often? |
| Link destinations | Were repeated prompts offering the same action or different choices? |
| Images and alt text | What visual structure was present, and what alternative text existed? |
| Interpreted meaning | Did the copy explain delivery or offer help choosing? |
| Offer and timing | What price and delay were actually proposed? |
| Exact source and provenance | Which document and settings produced this observation? |

This is **vectorising the email for an analysis we care about**. We are not trying to make one vector capture everything an email could mean. We choose relationships relevant to the intervention, retain the evidence that produced each reading, and keep the source available when those choices need revisiting.

There are two representations in the loop. The behavioural representation describes the recipient's evolving situation. The intervention representation describes the message and its settings. We learn from observations connecting the two to the subsequent trajectory and endpoint.

A small number of named parameters makes that connection easier to inspect. “Variant A” identifies one exact treatment. “Five-percent reduction, later CTA, delivery explained, forty-minute delay” describes properties that other variants can share.

Those properties do not replace identity. Two documents can have the same coarse vector while differing in wording, facts or details we failed to represent. Exact versions let us discover that the representation missed something rather than quietly pooling incompatible content.

# The Same Destination Can Be Offered in Different Ways

Links are a good example of why structure matters.

Suppose the main button and a closing reminder both point to the boots. Recording only the destination loses which placement received the click. Recording only the clicked text loses whether it appeared before or after the explanation.

The example therefore retains every usable link occurrence: its own ID, label, original URL, logical destination, region and position in the extracted text. A header link, a body CTA and an unsubscribe link have different roles even if all three are ordinary anchors in HTML.

The captured control email contains:

| Occurrence | Region | Text position | Label |
|------------|--------|--------------:|-------|
| `link-01` | Header | 0.000 | Fieldwork |
| `link-02` | Body | 0.852 | View the walking boots for £100.00 |
| `link-03` | Footer | 0.983 | Unsubscribe |

The position is the fraction of extracted body text preceding the link. A second captured version places its primary CTA near `0.201`. That is a measurable structural difference between messages about the same product.

It is not a measurement of pixels or “above the fold”. Email clients, table layouts, responsive styles and images can change visual geometry. The current producer measures source-text order. A rendered-position producer would need a declared viewport, client/rendering environment and its own version.

The analyser also distinguishes a link's identity from its canonical destination. It removes known `utm_` tracking parameters for destination comparison while preserving meaningful parameters such as a product ID. Two occurrences can then be recognised as the same action offered twice without collapsing their click attribution.

Primary-CTA selection is deliberately inspectable. The sample excludes recognised header/footer and opt-out links, prefers button-like body links and then uses text order. That is a replaceable heuristic, not a universal understanding of marketing intent. A newsletter with several equally important actions may need a different characterisation.

A missing CTA is not encoded as “CTA at position zero”. This dense-vector email family rejects it. Missing evidence and a measured value of zero remain different, as in [Part 1.5](/blog/show-me-what-you-got-part1-5-questions-as-coordinates).

The consequence for experimentation is practical. If repeated links help one detected pattern but distract another, we have a feature and occurrence-level evidence with which to investigate it. If we retained only one whole-document embedding and one destination-level click count, that relationship would be much harder to see.

# Give the Intervention Comparable Coordinates

The sample stores separate structural, meaning, timing and offer vectors. The effect estimator starts with five selected action coordinates:

```csharp
var intervention = new[]
{
    discountFraction / 0.10,
    primaryCtaTextPosition,
    deliveryExplanationReading,
    delayMinutes / 60.0,
    productDestinationOccurrences / 4.0
};
```

This code expresses the modelling decision. A five-percent reduction becomes `0.5` on an axis whose declared maximum is ten percent. Forty minutes becomes about `0.667` on a sixty-minute timing scale. One occurrence becomes `0.25` under the repeat-link scale.

The captured examples look like this:

| Property | Existing version | Another version |
|----------|-----------------:|----------------:|
| Price | £100 | £95 |
| Primary CTA text position | 0.852 | 0.201 |
| Delay | 40 minutes | 10 minutes |
| Delivery-explanation reading | 0 | 0 |
| Product-destination occurrences | 1 | 1 |
| Effect descriptor | `[0, .852, 0, .667, .25]` | `[.5, .201, 0, .167, .25]` |

These particular readings use the reproducible lexical producer. The full document remains available alongside the vector, so we can examine why an observation received a value.

A relative change is useful for transfer. Five percent can describe offers on differently priced products, while the exact currency and amounts remain recorded. It does not imply that five percent produces the same response for every product, recipient or starting situation.

Likewise, a text-position fraction lets us compare layouts of different lengths. It does not make two CTAs at `0.2` psychologically equivalent. The coordinate is a testable input, not a certificate of shared effect.

> **ML aside: feature engineering is choosing what the learner can notice.** Normalisation gives each coordinate a declared scale. It matters for both distance comparisons and the coefficient penalty we will use later. Action descriptors also let the learner share evidence: an unseen email can share price and placement characteristics with observed ones. This is **generalisation**. It works only as far as those characteristics preserve the distinctions that affect response. A document ID alone offers no such relationship between new and old versions.

The descriptor must be versioned. Changing the question rubric, canonicalisation rule, feature scale or meaning of a coordinate changes the space. Reusing an array length or a field name does not preserve compatibility. [Part 2's storage boundary](/blog/show-me-what-you-got-part2-building-relevance#a-small-storage-boundary) makes that contract concrete.

This is also why the exact intervention includes settings outside the copy. An otherwise identical email sent at a different time is a different treatment. Preserve intended settings and evidence of what was actually applied, rather than assuming a generator's request became a delivered message unchanged.

# Where a Decision Model Helps

Some properties are directly measured. A configured price is not something we need an LLM to guess. Whether a message explains delivery or offers help choosing may require interpretation.

The sample's cheap baseline checks literal phrases. It is explicitly named `literal-phrases`, so nobody mistakes a substring match for semantic understanding. It is useful for repeatable experiments, but wording such as “shipping is included” can express the same idea without containing “delivery costs”.

A decision model can supply that interpreted coordinate. The questions define the property, rather than asking the model to judge whether the email will work:

| Question | What a positive reading means |
|----------|------------------------------|
| Does the email explain delivery costs or availability? | It states charges, free delivery, or where charges can be checked |
| Does it offer help choosing or comparing? | It explicitly offers assistance with that decision |

In the captured Nimble analysis, the control email received approximately `0.0154` for delivery explanation and `0.9950` for offering help. Those are actual outputs over the source, with the model digest retained. They are not probabilities that a recipient opened, clicked or purchased.

That distinction is the connection back to [Part 1.5](/blog/show-me-what-you-got-part1-5-questions-as-coordinates). A classifier reading becomes a feature. Its downstream usefulness has to be measured through the response, not supplied by asking the same model whether its judgement was persuasive.

The producer also keeps availability. A failed or missing reading does not become “property absent”. The implementation validates the requested answers and separates model-based and lexical experiments into different versioned scopes.

Structural and interpreted features complement each other. The structural producer can identify the button occurrence and its position. The decision model can interpret the wording around it. Neither needs to pretend to do the other's job, and neither can establish what the recipient actually did.

The readings are computed when a variant is created or changed. They can be reused across recipients while the response stream continues to update. That keeps the expensive interpretation step separate from the immediate experiment decision.

> **ML aside: there are two different prediction tasks here.** The content classifier estimates whether a property is expressed in the email. The response model estimates the reward after an intervention in a particular context. A very confident answer to “does this explain delivery?” is not evidence that explaining delivery will help. We borrow a trained model to produce a feature, then learn whether that feature is useful from outcomes.

# A Rewrite Is a Proposal, Not an Improvement

A human might move the CTA, clarify delivery or shorten a paragraph. An LLM might restructure or reword the same source. Both produce a new document to characterise.

The intervention is the resulting document and settings, not the instruction “make this clearer”. We analyse what actually came back: its links, source order, interpreted properties and exact content identity. If the model failed to change the property we intended to vary, that should be visible before we run the experiment.

The example app supports both routes. Human edits and model rewrites enter the same analysis/review/registration boundary. The captured small-model rewrite came from `qwen3.5:0.8b`; its actual output is retained with the sample. The model proposes content and does not register or deliver it automatically.

Observable checks retain approved destinations and visible prices, while review covers facts and meaning those checks cannot establish. Registration creates a new immutable candidate. It does not overwrite the version whose earlier responses trained the system.

A generated revision can therefore be structurally similar to earlier successful messages, receive a promising estimated score, and still perform poorly. That is a useful result. It can reveal a missing feature, a poor semantic reading, a context interaction or an ordinary unsuccessful intervention.

This is how we progressively improve the characterisation as well as the content. If two variants have nearly identical descriptors but consistently different effects, the representation may be omitting something relevant. We can inspect their source differences, propose another coordinate, then test whether that addition improves later decisions.

Adding that coordinate is not the same as proving its relevance. A readable dimension and a plausible story still need comparison against observed outcomes. The vector is a model of the intervention, and it can be revised like any other model.

# Recommend the Emails Themselves

The existing recommendation system can now operate on interventions.

An email is a candidate with named structural, semantic, timing and offer vectors. A detected context is associated with observations of which email versions produced useful outcomes. Those observations provide a bridge between recipient behaviour and message properties.

```text
observed behaviour → detected context
                         ↓
             email versions and observed responses
                         ↓
           properties associated with useful outcomes
                         ↓
                 candidate email ranking
```

We do not directly compare a shopper's comparison-rate vector with the email's link-position vector. They describe different things. The association comes from the response history: what happened when this kind of message was used in this kind of situation?

Positive-margin observed outcomes update the recommender's ordinary per-subject summaries, using the detected group as the subject and the exact email as the candidate. Future emails can be compared with those accumulated properties through the same distance and rank-fusion machinery explained in [Part 2](/blog/show-me-what-you-got-part2-building-relevance).

That gives us a useful candidate opinion: this version resembles messages associated with helpful responses in this context. It is especially interesting for new revisions, whose characteristics may overlap with earlier versions despite having a new content ID.

Similarity is still not an effect estimate. A summary built from positive outcomes does not contain the denominator of every failed or unobserved assignment. The learning layer retains those counts and objective values. Retrieval and experimental evidence have complementary responsibilities.

The implementation uses the existing ranker without creating another core feedback-attribution record on each experimental choice. The experiment owns the richer ledger containing the exact source, alternatives, pre-intervention context and realised selection probability. That is reuse of the relevance machinery, rather than a second disconnected email-recommendation system.

# Measure the Push, Not Just the Final Position

Return to the asteroid. Seeing it nearer a desired destination after a nudge is insufficient. Comparable objects may have been moving that way already.

For user journeys, we therefore retain the state before the intervention and observe the state afterwards. Over a declared interval, we can compare the movement with the average movement of comparable controls:

```csharp
var movementWithChange = treated.Average(journey =>
    journey.After[axis] - journey.Before[axis]);

var movementWithoutChange = controls.Average(journey =>
    journey.After[axis] - journey.Before[axis]);

var observedDeviation = movementWithChange - movementWithoutChange;
```

This code expresses the comparison, not a complete causal estimator. Both groups need compatible representations, comparable starting contexts and the same observation interval. Randomised assignment supplies the clean starting example. Historical matching alone can leave important differences unobserved.

> **ML aside: prediction is not uplift.** A purchase predictor asks who is likely to buy under the circumstances represented in its data. An **uplift** or treatment-effect model asks how an intervention changes the outcome compared with a specified alternative. Someone already likely to buy may benefit very little from a discount. Someone less likely to buy may respond strongly. We want to learn the difference the push makes, which is why a good forecast alone cannot choose the best intervention.

We cannot observe the same person's simultaneous future both with and without that email. The missing alternative is the **counterfactual**. Randomised groups let us estimate average differences; conditioning on relevant starting behaviour lets us investigate where those differences vary. This is the territory of heterogeneous treatment effects, described in [EconML's motivation](https://www.pywhy.org/EconML/spec/motivation.html). Our small reward regression is a starting response model, not an implementation of those more advanced causal estimators.

For richer trajectories, the same comparison can include direction, speed, acceleration, time to a state and destination rates. A message may accelerate a purchase that would have happened anyway, reduce cancellation, or move someone into more comparisons without increasing eventual purchases. Those are different responses, and the objective determines which are useful.

The best visualisation keeps these observations distinct. Show the pre-intervention track, the assignment marker, the actual application/exposure evidence and the later measured points. Keep comparable control tracks or their distribution visible. A predicted continuation can be shown separately, but must not masquerade as a measured untreated journey.

An email's coordinates describe the attempted push. The deviation belongs to the recipient's behavioural coordinates. We learn a relationship between those spaces. Subtracting an email vector from a user vector would not produce a meaningful “persuasion distance”.

The example records pre/post context and actual endpoints. Its synthetic post-context values are generated by the known response environment; they are not independent human trajectory measurements. The destination and reward comparison is the reported experiment result. A production journey visualiser would obtain each follow-up point from the application's event stream.

That qualification matters because two correlated views of the same simulated endpoint do not become two independent proofs of effect. The purpose of the fixture is to make the update and comparison inspectable, then give real observations a clear place to enter.

# Did the Nudge Reach the Person?

The response has its own sequence. Assignment is not delivery, delivery is not viewing, and viewing is not necessarily engagement.

A selected email may never be sent. A provider may report a delivery failure. A message may be delivered without any usable exposure telemetry. A recipient may click a specific occurrence without a reliable open event. Each case should survive as evidence rather than being squeezed into one success flag.

The lab records these stages separately. A first-party preview journey can read:

```text
delivered → viewed → clicked / link-02 → outcome
```

That is a way to exercise the observation contract in a local inbox, not a real mail dispatch or a measurement of ten or forty elapsed minutes. The seeded environment supplies the controlled timing experiment; production integration must report actual delivery/application times and settings.

“No observed view” is the useful label when view telemetry is absent. It is not proof that the email was unread. Apple's [Mail Privacy Protection explanation](https://www.apple.com/legal/privacy/data/en/mail-privacy-protection/) describes background downloading of remote content regardless of engagement, so a pixel request does not establish a human open either.

A decision model analysing the email cannot answer whether its recipient read it. That requires response evidence from the relevant system. A click, reply or acknowledgement supplies another observation, while complete comprehension remains a different claim.

This fits the nudge metaphor: the system needs evidence about application as well as the proposed change. But uncertain application does not justify discarding the assigned recipient. Failure to deliver or engage may be part of the policy's real practical result.

The assignment-level comparison therefore retains people with no observed view whenever their endpoint is observed. Restricting the analysis to readers afterwards would answer a different question about a selected group. It would also hide whether the message, timing or channel failed to reach people usefully.

Missing endpoint telemetry is different again. It becomes censored, not an invented zero-reward outcome. A confirmed closed episode without purchase is observed evidence; a stream that simply stops is not a confirmed departure.

# Decide What Counts as a Useful Deflection

A greater purchase rate is not automatically the best result. A deep discount might improve conversion while reducing contribution. An email might attract clicks but generate complaints or unwanted contact. An operational action might improve latency by rejecting useful work.

The sample chooses a simple measurable objective: product margin relative to the original possible margin.

```csharp
var margin = purchased ? paid - originalPrice * 0.55m : 0;
var reward = (double)(margin / (originalPrice * 0.45m));
```

A full-price purchase receives a reward of `1`. A discounted purchase contributes less margin. An observed closed episode without purchase receives zero; an unknown endpoint supplies no fabricated reward.

The exact paid amount remains recorded, including orders without a tracked offer click. Raw margin and contact cost are available beside the bounded reward, so the scalar does not erase the components that explain it.

The email variants have the same contact cost in this fixture. A later send/no-send policy needs that control and an objective including its differing costs. Choosing the best email for an eligible situation and deciding whether contacting someone helped are related but different experiments.

The time window is part of the target too. Part 3's observation-anchored forecast and an intervention's assignment/application windows cannot be mixed casually. An email planned for much later cannot be judged against a purchase prediction whose horizon ended before it arrived.

The intervention ledger retains assignment, stage and endpoint times. Comparing treated and control journeys uses the declared common window; secondary response windows can examine what happened after verified exposure. Those clocks and populations need to stay visible.

# Learn Which Characteristics Tend to Help

The first learning result is a table of observed outcomes by detected context and exact email version. It retains assignment counts, completed endpoints, censoring, views and clicks separately.

The sample also learns a small descriptor-conditioned reward model within each detected group. It starts with a baseline value and adds weighted contributions from the five selected action coordinates:

```csharp
var estimatedReward = coefficients[0]
    + descriptor.Select((value, i) =>
        value * coefficients[i + 1]).Sum();
```

Read this as “given this context, how do these intervention characteristics relate to the observed reward?” The weights come from completed observations. They are not the decision model's judgement that the copy sounds effective.

The implementation maintains the sufficient statistics for a small ridge regression. That lets it update incrementally without replaying every earlier message or invoking a neural training pipeline. The penalty stabilises the estimate; it does not make every coefficient an isolated causal effect.

> **ML aside: this is already a trained model.** Regression learns weights from examples with observed targets. Here the inputs are intervention characteristics within a detected context, and the target is realised margin reward. “Linear” means the prediction adds weighted feature contributions. “Ridge” adds a penalty for large weights, reducing the temptation to explain limited or correlated evidence with extreme coefficients. The [scikit-learn linear-model guide](https://scikit-learn.org/stable/modules/linear_model.html#ridge-regression-and-classification) explains the same technique. No neural network is required.

The interesting training code accumulates the evidence needed to fit those weights:

```csharp
var x = new[] { 1.0 }.Concat(descriptor).ToArray();
for (var i = 0; i < x.Length; i++)
{
    Target[i] += x[i] * reward;
    for (var j = 0; j < x.Length; j++)
        Gram[i][j] += x[i] * x[j];
}
```

The leading `1.0` supplies the baseline term. `Target` accumulates how each input varies with reward; `Gram` accumulates how inputs vary together. These running totals are **sufficient statistics** for this least-squares fit: with the penalty, they let us solve for the coefficients without retaining every training row in the fitting algorithm. We still retain experimental evidence separately for audit and evaluation.

That is training directly from a stream of completed observations. It does not mean the model is automatically responsive to drift: this baseline accumulates old and new evidence together. Forgetting factors or rolling windows would be additional modelling choices when yesterday's response stops describing today's population.

The additive model also makes an assumption we can inspect. A discount and early CTA contribute separately within each group. If their combination matters in a way neither feature captures alone, we could add an interaction feature such as `discount * earlyCta`, or try a more flexible learner. More flexibility needs more evidence. We would compare later performance rather than assume the more complex model wins.

The model can give a new version a provisional score using characteristics it shares with observed versions. It only does so after sufficient outcomes and near supported descriptors. Similarity does not manufacture completed trials. The returned explanation still says how many actual outcomes exist for the exact candidate and whether the score came from descriptor regression, an observed mean or only relevance evidence.

This is the route towards learning useful content, timing and targeting by emergent behaviour. An unseen version is not necessarily an unrelated arm, but nor is it automatically a proven improvement. Sharing evidence through parameters is a hypothesis about the representation's usefulness.

The policy starts with uniform assignment, then uses an exploration fraction alongside its preferred choice. Every eligible action, including the control, retains a positive chance. Otherwise the system could reinforce an early preference and stop collecting evidence about alternatives.

> **ML aside: this is a contextual-bandit problem.** The context is the observed journey, the action is one eligible email version, and the reward is its later measured result. We observe feedback for the chosen action; the other versions do not acquire imaginary outcomes. A **policy** is the rule that turns the current evidence into a choice. This framing and its exploration strategies are introduced in [Vowpal Wabbit's contextual-bandit tutorial](https://vowpalwabbit.org/docs/vowpal_wabbit/python/latest/tutorials/python_Contextual_bandits_and_Vowpal_Wabbit.html).

Our baseline uses **epsilon-greedy** exploration: mostly prefer the currently best-scoring action, but sometimes choose uniformly to keep learning. After warm-up, the probability of selecting a particular action is:

```csharp
var probability = exploration / eligibleCount
    + (isPreferredAction ? 1.0 - exploration : 0.0);
```

With exploration `0.25` and 48 eligible versions, each receives about `0.52%` probability through exploration. The preferred version also receives the `75%` exploitation share. Those are assignment chances, not predicted purchase probabilities. Warm-up instead assigns each version `1 / 48`.

A contextual bandit learns which single choice tends to help in a context. A fuller **reinforcement-learning** problem would model a sequence of decisions: this email changes the state in which tomorrow's offer is selected, and the policy must consider longer-term consequences. A delayed reward alone does not make our example that fuller sequential learner.

The realised selection probability is recorded when the decision is made. It is neither the candidate's RRF score nor its estimated reward. Keeping those numbers separate lets a later evaluator understand how the observations were selected.

> **ML aside: the policy changes its own training data.** Once a preferred version gets more traffic, its raw outcome count cannot be compared fairly with a rarely selected alternative. Logged assignment probabilities are useful for methods such as inverse-propensity evaluation, which reweight observations to account for their selection chances. Logging is the prerequisite, not the estimator itself. This sample reports a separate randomised policy comparison; it does not claim to implement that off-policy evaluation.

For the five relevant quantities, the meanings are:

| Quantity | What it means |
|----------|---------------|
| Semantic reading | Model-assigned support for a property of the content |
| Relevance score | Candidate order under the configured representation/ranking policy |
| Expected reward | An estimate under the declared objective and observed context |
| Assignment probability | The chance the policy gave this action at that decision |
| Observed outcome | Evidence about what actually happened afterwards |

A sophisticated representation does not remove the need for that separation. It makes the information available; the experiment decides whether using it improves results.

# What the Measured Example Shows

The companion example generates 48 actual email documents. It varies price, CTA placement, delivery explanation, delay and repetition of the product destination. The detector receives observed visit coordinates, while independent random streams govern assignment and the synthetic recipient response.

The population contains 2,400 training visits and 1,200 later evaluation visits. After the run, the detector has two groups of 1,800 context observations each. No audience label was supplied as an input.

To inspect one characteristic, consider delivery explanation during the uniformly randomised training phase. The other generated factors also vary, so this is an average comparison over those alternatives, not a pair of otherwise identical hand-picked emails:

| Context | Explanation | Endpoints | Purchase | Reward |
|------------------|----------------------|-------------------:|--------------:|-------------------:|
| Higher | Absent | 559 | 25.9% | 0.2365 |
| Higher | Present | 587 | 38.0% | 0.3523 |
| Lower | Absent | 549 | 27.5% | 0.2568 |
| Lower | Present | 590 | 23.4% | 0.2186 |

“Higher” and “Lower” refer to comparison activity and price narrowing. Purchase rates and mean rewards use observed endpoints.

That is the kind of context-dependent relationship we wanted to make visible. In this response environment, adding the explanation points in different directions for the two patterns. It is not a universal rule that longer explanations help careful shoppers or harm direct shoppers.

The learned coefficient directions reflect the observed fixture too. Delivery explanation and longer delay have positive coefficients for the first group and negative coefficients for the second. Earlier CTA placement is favoured by the second group's estimate. These are properties of this run and model, not claims about an ideal marketing email.

In the later population, recipients are assigned between uniform and adaptive policies:

| Policy | Assigned | Endpoints | Censored |
|--------|---------:|----------:|---------:|
| Uniform | 565 | 526 | 39 |
| Adaptive | 635 | 609 | 26 |

Among those observed endpoints:

| Policy | Purchase rate | Mean margin reward |
|--------|--------------:|-------------------:|
| Uniform | 25.5% | 0.2385 |
| Adaptive | 41.9% | 0.3881 |

Each choice precedes that recipient's outcome. The adaptive policy continues updating from earlier completed episodes, so this is an online evaluation rather than a frozen-model benchmark.

The fixture's missing-endpoint mechanism is random. Real missing feedback may be selective, which can bias a complete-case average. The table retains censoring instead of implying that every assigned visit supplied an endpoint.

No-observed-view cases are retained as well. Across the run, 1,635 scored episodes had no observed view. In the later population, the uniform and adaptive arms included 283 and 284 no-observed-view assignments respectively, including any with censored endpoints.

These are genuine program outputs from a synthetic response environment. The policy comparison uses the reproducible lexical producer; the separately captured Nimble readings do not become an invented claim that adding an LLM improved conversion. The exact documents, readings, rewrite and measured output are retained in the [sample artifacts](https://github.com/scottgal/mostlylucidweb/tree/main/samples/RelevanceLearning/artifacts/seed-814).

> **ML aside: what would count as learning something useful?** New recipients test decisions made before their outcomes arrive. New email variants test generalisation across content. A later season or changed product range tests whether the relationship survives a changed environment. These are different questions. The synthetic run verifies that the machinery can recover useful structure in its specified environment; it does not establish real marketing effectiveness. The next evidence must come from measured interventions in the intended setting.

# Keep the Experimental History Intact

The relationship between a proposed push and its response only remains useful if we retain what actually happened at the time.

A new human or model revision gets a new identity. An assignment snapshots its exact source and descriptor, pre-intervention context, detected group, alternatives, policy and probability. Retries return the original assignment instead of quietly giving the same visit another treatment.

Outcome deduplication and the learning update commit together. An identical event does not train twice, and a conflicting reuse is rejected. Final outcomes cannot be counted again under another ID. Pending or censored feedback does not acquire a convenient reward merely because time passes.

The positive-response bridge into the recommender uses a persistent outbox. If that separate affinity update fails after the outcome commits, its stable event identity can be replayed without duplicating the authoritative outcome. We preserve the observation rather than pretending two stores completed one distributed transaction.

The captured reopen check confirms that groups, outcomes and exact versions survive a restart: two detected groups, 3,420 observed outcomes and 48 variants. The test suite also checks opposite context preferences and a descriptor-based estimate for a new variant that still has zero observed outcomes of its own.

These mechanics support the explanation; they are not the purpose of the experiment. The useful history is the connection between context, actual intervention, application evidence, subsequent trajectory and result.

# The Same Nudge Can Be an Operational Change

For a service, the proposed push might be a rate-limit or concurrency change. Its descriptor records the previous value, new value, ratio, scope and duration. The service's state and movement supply the context.

The question is again whether subsequent behaviour deviates usefully from comparable incidents without that change. Returning towards a normal centroid is evidence to examine, but a service might have recovered anyway. A lower latency caused by rejecting most requests may also be an undesirable outcome.

The existing demo makes a real reversible rate-limit change and keeps before/current observations. That notebook is not a controlled recovery experiment. The generic intervention contracts can retain verified application and subsequent measured outcomes; a production integration needs an incident-level comparison and declared objective before learning which operational changes help.

Different domains supply different meanings and executors. The shared idea is the relationship we are learning: what kind of change, in what kind of evolving situation, produced what subsequent deviation?

# Better Content, Timing and Targets

The aim is progressively better interventions for the behavioural patterns the system detects. That may mean a clearer explanation for one group, a shorter route to the product for another, a different delay, or evidence that contacting someone is not worthwhile under the stated objective.

A human and an LLM can both supply the next candidate. Neither gets to declare that it is better. The system characterises the actual version, records its application and learns from measured responses against the relevant alternative.

The asteroid picture keeps the purpose clear: we observe a course, try a nudge and compare what follows with similar courses without it. The email vector describes the intervention we attempted. The observed journey and endpoint tell us whether it helped.

The [working email lab](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid.Relevance.Demo) lets us inspect those stages. Its [sample README](https://github.com/scottgal/mostlylucidweb/tree/main/samples/RelevanceLearning) contains the run commands, model-host configuration and source map. The article's examples come from that code; the code is there to make the model and its evidence inspectable.
