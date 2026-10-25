# SHOW ME WHAT YOU GOT (Part 4) : Mind Contol - The Subtle Art of Manipulation.

The “mind control” in the title is a joke about a familiar ambition: put the right thing in front of someone at the right moment and change what they do next. Most of us have ignored enough perfectly timed marketing emails to know how unreliable that ambition can be.

But segmentation and learning really are used to try to manipulate many aspects of modern life: what we buy, which adverts reach us, what we watch next, which messages get our attention and whether we keep a subscription. Choosing what to show, when to show it and what to measure afterwards is a way of trying to influence behaviour.

These systems can help someone find a suitable product or understand a choice. They can also serve an objective that conflicts with the person's interests. Meta's [feed-ranking explanation](https://ai.meta.com/tools/system-cards/instagram-feed-ranking/) describes ranking around predicted interactions. The FTC's [Bringing Dark Patterns to Light](https://www.ftc.gov/reports/bringing-dark-patterns-light) documents examples where interfaces are designed to steer or obstruct consumer choices. Calling an optimisation target “engagement” or “conversion” leaves us with the practical question: whose result are we improving?

Our examples will keep that objective visible. A retailer might want profitable purchases without unwanted contact. A service operator might want recovery without rejecting all the useful traffic. A higher number on one dashboard is not enough to describe either result.

In [Part 3](/blog/show-me-what-you-got-part3-predict), we characterised a journey and estimated where it was heading. Now we want to introduce a change, observe the response and use that experience when choosing the next change. Before we get to adaptive intervention models, there is a familiar place to begin: audience segmentation and experiments.

<datetime class="hidden">2026-10-25T08:00</datetime>
<!-- category -- AI,Architecture,Machine Learning,Recommendations,Behavioural Inference,C#,Experimentation -->
<!-- Working draft: the Part 4 Learning extension and experimental replay are planned. Do not present examples as captured implementation output. -->

[TOC]

# Start With a Segment and a Control

Suppose we define an audience of returning shoppers who have compared walking boots but have not completed checkout. We can try a different offer for some eligible sessions and keep a control group for comparison.

The first experiment can be deliberately small:

| Variant | Price of the same product | What changes |
|---------|--------------------------:|--------------|
| Control | £100 | Existing offer |
| A | £95 | Price only |
| B | £90 | Price only |

Keep the product, placement and copy stable. Decide how sessions enter the experiment, assign a variant, record what was actually shown, and measure the defined outcome over the same window. “People in the cheaper group bought more” needs a fair comparison, including margin and the people who did not engage.

This is familiar experimentation. Systems such as [Firebase A/B Testing](https://firebase.google.com/docs/ab-testing/abtest-with-console) already combine audience targeting with baseline and variant comparisons. Its [configuration experiments](https://firebase.google.com/docs/ab-testing/abtest-config) can vary parameters too. Our next step connects those ideas to the streaming characterisations we have been building throughout the series.

# Characterise the Change As Well As the Shopper

So far, the changing entity has had a representation: interests, current position, movement and likely destinations. Give the intervention a representation too.

Instead of retaining only “variant A”, retain what A actually did: a £95 price, a £5 reduction from the original offer, the channel and placement where it appeared, and the time it was introduced. Its ID identifies the exact treatment; its parameters describe the treatment.

For an approved price set, generating variants is ordinary code:

```csharp
var originalPrice = 100m;
var allowedPrices = new[] { 100m, 95m, 90m };
var variants = allowedPrices.Select(price => new
{
    Family = "checkout-offer",
    PriceGbp = price,
    DiscountFraction = (originalPrice - price) / originalPrice
}).ToArray();
```

The application supplies the allowed prices. The generator produces candidates; the experiment chooses among them. Recording the fraction as well as the amount makes a five-percent reduction recognisable across differently priced products, while the original amounts and currency remain available.

Some properties are measured directly. Others need interpretation. [Part 1.5](/blog/show-me-what-you-got-part1-5-questions-as-coordinates) gives us a producer for the latter: ask a suitable decision model whether a message explains delivery costs, mentions a real deadline or asks for a particular next action. Retain those readings with their question definitions, model versions and availability. Characterise a variant when it is created or changed, then reuse that descriptor when selecting it for different sessions.

This gives us comparable parameters within a defined intervention family. It does not put an email, a product discount and a rate limit on one universal scale. Each family needs meaningful dimensions, units and a versioned schema.

The longer-term learning problem can then use both descriptions: **what situation were we in, and what kind of change did we make?** A new price or message need not be treated as an entirely unrelated label. Whether shared parameters support useful generalisation is something to evaluate, rather than assume from their vector distances.

# Follow the Intervention Through to the Response

A selected email is not necessarily sent. A sent email is not necessarily delivered. Delivery does not establish that a person read it. The event stream should preserve those stages, including unknown or missing observations.

That matters for “didn't read the email” detection. We can record **no observed open** within a window, but that does not prove unread content. Apple's [Mail Privacy Protection explanation](https://www.apple.com/legal/privacy/data/en/mail-privacy-protection/) describes background downloads of remote content regardless of engagement, so a tracking request does not reliably establish a human open either. A click, reply or first-party acknowledgement supplies different evidence; none establishes complete comprehension.

Measure the whole assigned group as well as the observed engagement stages. Dropping everyone who did not open would change the question to one about the people who engaged after assignment. A no-contact control also needs the same follow-up window, even though it has no email exposure time.

The useful sequence is:

```text
context at assignment + exact intervention parameters
    → assignment
    → delivery or application evidence
    → observed engagement, or unknown exposure
    → subsequent trajectory and actual endpoint
    → comparison with the eligible alternative
```

We can measure movement towards or away from the desired destination after the change. We can also measure deviation from a pre-intervention forecast, only when the forecast and measurement use the same target and time window. Neither comparison alone proves that the intervention caused the movement. The control gives us a reference for what happened without that particular change.

# The Same Experiment Can Change a Rate Limit

For a service, the intervention might move an allowed request limit from 240 to 180 or 120 requests per minute. Its descriptor records the previous limit, the new limit, their ratio, scope and duration. The application reports whether the change actually took effect.

Then observe the trajectory: latency, errors, queue growth, throughput and rejected work. Did the service settle towards its qualified normal reference? Did recovery last? Did the apparent improvement come from refusing useful requests?

These are responses to measure against an appropriate control or alternative across comparable incidents. A recovery trace replayed after clicking a button is not evidence that the button caused recovery.

The planned `.Learning` extension will retain that experience and use it to update future choices among allowed interventions. We begin with inspectable comparisons and statistics, then evaluate richer policies that use the intervention's parameters as well as the entity's context.

<!-- Remaining implementation sections and evaluation design: docs/relevance/part4-intervention-learning.md. Replace planned language only when the code and captured experimental output exist. -->
