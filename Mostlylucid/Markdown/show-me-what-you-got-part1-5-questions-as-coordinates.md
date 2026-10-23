# SHOW ME WHAT YOU GOT (Part 1.5): Questions as Coordinates

In [Part 1](/blog/show-me-what-you-got-streaming-recommendations), we chose representations that preserve useful relationships. A colour swatch gives us colour coordinates. A session's events give us behavioural features. A text embedding places language in a learned semantic space.

There is another useful way to characterise an entity: **define the dimensions as questions, then use the answers as coordinates**.

Does this message request credentials? Does it introduce a new payment destination? Does it pressure the recipient to act immediately?

Those questions already exist in StyloMail. Their answers can become a small, explicit semantic vector. We can then use the relevance engine from [Part 2](/blog/show-me-what-you-got-part2-building-relevance) to compare that characterisation with useful review tasks.

That gives the two projects a practical connection. StyloMail interprets the message and retains the evidence. Relevance ranks eligible candidates from the resulting representation. The same pattern can work over product descriptions, support requests or diagnostic reports, provided we define what the questions mean and what their answers justify.

<datetime class="hidden">2026-10-23T15:00</datetime>
<!-- category -- AI,Architecture,Machine Learning,Behavioural Inference,Recommendations,StyloMail,Jev,Nimble,C# -->

[TOC]

# A Dimension Can Be a Question

Take this message:

> We are changing banks next month. Please verify the replacement details by calling your usual accounts contact. No payment is needed today.

The payment-change condition is present. Urgent pressure is absent. A request for credentials is absent.

Compare it with:

> Transfer today's payment to the replacement account in the next twenty minutes. Do not contact accounts until it is done.

Again, the payment-change condition is present. This time urgency is present too, and a further question about bypassing normal verification would be useful.

The shared topic is not enough to distinguish the two situations. We want coordinates describing particular properties of the request.

[StyloMail's semantic dimensions](https://github.com/scottgal/stylomail/blob/d4d60cc77d22e112ac11fbbba83dee65161cb185/src/StyloMail.Core/SemanticDimension.cs) define those properties. The payment question contains:

```csharp
Id = "semantic.payment_redirection",
Instructions = "Does this message attempt to introduce or change payment destination details?",
CriteriaTrue = "Supplies or changes bank account, routing, invoice or payment destination details.",
CriteriaFalse = "No introduction or change of payment destination.",
```

The question has a specific job. It does not ask whether the sender is fraudulent or whether payment should be allowed.

The stable ID lets code recognise the result. The instruction and criteria define what is being judged. Changing the criteria changes the dimension, even if the ID stays the same.

> **ML aside: an explicit semantic feature.** A normal embedding model produces latent coordinates whose individual meanings we generally do not name. Here, we choose the meaning of every coordinate. A model still interprets the language, but its output occupies a space defined by our questions.

# The Decision Model Supplies the Reading

StyloMail already has adapters for Jev and Nimble. The [Jev article](/blog/stylomail-behavioural-inference-with-jev) explains the hosted path; the [Nimble article](/blog/stylomail-using-nimble-from-csharp) follows the local C# call.

For a yes/no property, the typed question is **Noul**. It returns a number in `[0,1]` representing the model's probability of yes. Its instruction and true/false criteria define the condition. Noul does not include a separate confidence field. [TypeSafe's Noul documentation](https://docs.typesafe.ai/primitives/noul) describes that contract.

The existing Jev adapter turns dimension definitions into request questions:

```csharp
questions[dimension.Id] = new JevQuestion
{
    Type = NoulType,
    Instructions = dimension.Instructions,
    Criteria = new JevNoulCriteria
    {
        True = dimension.CriteriaTrue,
        False = dimension.CriteriaFalse,
    },
};
```

That is an extract from the real [`BuildQuestions` method](https://github.com/scottgal/stylomail/blob/d4d60cc77d22e112ac11fbbba83dee65161cb185/src/StyloMail.Jev/JevSemanticMailClassifier.cs). The surrounding adapter supplies the bounded message state, handles the request and validates the returned answers.

[Nimble exposes the same family of typed questions through Ollama](https://ollama.com/library/nimble). Interface compatibility does not make different models' readings interchangeable. Keep the producer and its version with the result, and evaluate the questions on examples relevant to the application.

> **Probability aside: a probability of which proposition?** A high payment-redirection answer means the model sees a payment-change request. A legitimate supplier update should score highly too. It does not become a fraud probability, and it is not a measurement of how much money is at risk.

A value near 0.5 is also meaningful: yes and no receive similar probability. It is not “medium urgency”. For ordered intensity, define a suitable scored rubric. It is not a missing answer either.

# Turn the Answers into Coordinates

For the integration sample, choose three existing StyloMail questions in a fixed order:

```csharp
var ids = new[]
{
    "semantic.credential_request",
    "semantic.payment_redirection",
    "semantic.urgency_pressure"
};

var questions = ids.Select(id =>
    SemanticDimensions.All.Single(q => q.Id == id)).ToArray();
```

A complete assessment can then produce a vector such as:

```text
[credential request, payment redirection, urgency pressure]
[             0.034,               0.988,            0.012]
```

Those are deterministic fixture values in the new sample, not a fresh model measurement. They resemble the legitimate supplier-change case: strong payment-change evidence and little urgency.

The coordinates retain different conditions. Several can be high together, so we do not force them to sum to one. Evaluating questions separately also does not make their answers statistically independent; the same phrasing may influence several readings.

This is a **task-defined semantic feature vector**. It is useful for the properties we asked about. It does not preserve all the relationships a general text embedding might capture.

Nor can we compare it directly with an EmbeddingGemma vector. A shared array length would not create a shared meaning.

## More Than Mail

The questions can describe other domains:

| Entity | Possible dimension | What it describes |
|--------|--------------------|-------------------|
| Support request | Does the customer explicitly ask for a person? | A request present in the supplied conversation |
| Product description | Does the description claim waterproofing? | A claim in the description, not a certified property |
| Diagnostic report | Does the report describe increasing retries? | Evidence in the report, not a direct measurement of the service |
| Document | Does this clause require notice before cancellation? | An interpretation of the supplied clause |

We can define new dimensions without training a new model for each question. That makes experimentation approachable. It does not establish that a model can answer any question accurately, or infer facts missing from its input.

Use measured values when they are available. An actual retry count belongs in a numeric feature. A decision model helps interpret a report about retries when the relevant evidence is expressed in language.

# Unknown Must Survive the Conversion

The most consequential line in this kind of integration is often the one that defaults a missing answer to zero.

If the payment question timed out, zero would assert that no payment-change condition was observed. If all questions failed, a vector of zeroes could resemble a routine message. The provider outage would have manufactured reassuring evidence.

StyloMail already represents this distinction in [`EvidenceAvailability`](https://github.com/scottgal/stylomail/blob/d4d60cc77d22e112ac11fbbba83dee65161cb185/src/StyloMail.Core/EvidenceAvailability.cs). A signal can be available, unavailable, not applicable, or produced with reduced coverage.

Its existing [`EvidenceVectors.Semantic`](https://github.com/scottgal/stylomail/blob/d4d60cc77d22e112ac11fbbba83dee65161cb185/src/StyloMail.Assessment/EvidenceVectors.cs) conversion preserves those states in a masked `DimensionVector`:

```csharp
var vector = EvidenceVectors.Semantic(assessment.Evidence, ids);
```

An unavailable dimension is excluded from comparison rather than filled with a plausible-looking number. Coverage travels with the representation.

The current Relevance baseline uses dense vectors with fixed dimensions. It does not yet implement StyloMail's masked comparisons. The bridge therefore makes a smaller promise: **all selected questions must have usable answers, or the advisory ranking abstains**.

Its result reports the issue and supplies no recommendations. It does not ask the engine for a cold-start list and present that as evidence-based advice.

The projection checks each selected ID rather than trusting response order:

```csharp
var rows = assessment.Evidence
    .Where(e => e.SignalId == questions[i].Id
        && e.Origin == EvidenceOrigin.Semantic)
    .ToArray();
```

Exactly one matching semantic row must exist. Its value must be finite and within range, its availability complete, and its producer the expected version. Reduced or reason-tagged partial reads abstain too. A stale cached assessment also abstains in this sample.

An explicit zero passes these checks. Missing, zero and balanced uncertainty remain three different situations.

> **Coverage aside: complete is not certain.** Three valid answers give us complete coverage of these three questions. The answers may still be uncertain or wrong. Coverage is about what was produced, not whether the model's judgement has been validated.

# Questions Become a Representation Schema

Suppose we change the urgency question from “does it pressure the recipient?” to “does it mention a date?”

Both return one number. They can even use the same property name. But old and new vectors now describe different things.

The bridge fingerprints the ordered question definitions, their schema version, the expected evidence producer, preprocessing version and tenant identity. Changing any of those creates a separate engine scope. The full rubric participates, not just the array length.

Preprocessing matters because a model reading the whole conversation receives different evidence from one reading only the newest paragraph. A moving model alias also needs resolving or pinning before readings are mixed.

The calling host must supply versions from its actual classification configuration. An adapter cannot recover the wording of an earlier question from three returned numbers.

This connects back to Part 1's colour example. Encoding is part of the model of the problem. A coordinate's name is insufficient to establish its meaning.

# Give StyloMail a Relevance Component

Now we have something Relevance can consume: a complete, versioned characterisation of a message.

The next question is what to compare it with. The sample supplies review playbooks with prototype conditions:

| Playbook | Credential request | Payment change | Urgency |
|----------|-------------------:|---------------:|--------:|
| Verify supplier change | 0.01 | 0.99 | 0.02 |
| Review credential request | 0.99 | 0.01 | 0.99 |
| Escalate urgent payment change | 0.01 | 0.99 | 0.99 |

These are host-defined examples of conditions in which the task is worth considering. They are not learned outcomes or probabilities that the task will succeed.

The prototype and observation use the same question order and meanings. We can compare them with L2 distance. The question vector is stored using Relevance's existing `vector` converter; no new embedding runtime is required.

For example, the supplier fixture `[0.034, 0.988, 0.012]` is close to the supplier-verification prototype `[0.01, 0.99, 0.02]`:

```text
distance = sqrt(0.024² + (-0.002)² + (-0.008)²)
         ≈ 0.0254
```

A low distance here means similar answers to these selected questions. It does not establish that the message is legitimate or that a particular action causes a better result.

Ranking is relative to the eligible prototypes. The sample has no absolute distance threshold declaring a match good enough, so even complete evidence can return weak matches. Inspect the distances and evaluate that rejection policy before relying on the suggestions.

> **Geometry aside: keep magnitude when it means something.** Two question vectors can point in the same direction while assigning very different yes probabilities. Unit-normalising them would erase that distinction. The sample uses L2 on the original coordinates; the distance choice remains a hypothesis to evaluate.

## Relevance Fits Within the Existing Policy

StyloMail already has an explicit rule: probabilistic components produce evidence, and versioned policy selects a [`MailAction`](https://github.com/scottgal/stylomail/blob/d4d60cc77d22e112ac11fbbba83dee65161cb185/src/StyloMail.Core/MailAction.cs).

The review component respects that contract. It receives the action already selected by policy, then ranks playbooks eligible after that action. It returns task IDs such as `verify-supplier`, not a replacement `Allow`, `Reject` or `Quarantine` decision.

Inside the bridge, eligibility is an ordinary hard filter:

```csharp
var result = await engine.RecommendAsync(
    subject, "review",
    Filter.Eq("after", policyAction.ToString()),
    count: 3, cancellationToken: cancellationToken);
```

Even a perfect prototype match is excluded if it is only registered for a different policy action. This is where the distinction between relevance and authority becomes executable.

The composition point looks like this:

```csharp
SemanticAssessment semantic =
    await classifier.ClassifyAsync(input, cancellationToken);

// Existing policy combines semantic and deterministic evidence.
MailAction authorisedAction = policyDecision.Action;

MailRecommendation advice = await recommendations.RecommendAsync(
    assessmentId, authorisedAction, semantic,
    actualQuestionSchemaVersion, cancellationToken);
```

This is a composition sketch using StyloMail's existing contracts. The bridge itself is implemented in [`MailRecommendations`](https://github.com/scottgal/mostlylucidweb/blob/main/samples/RelevanceMail/MailRecommendations.cs). It is a working integration sample, not a component already installed in StyloMail's live host.

That is a useful role for Relevance inside StyloMail's core: a reusable advisory component for matching evidence to workflow candidates. Its value can be evaluated without replacing the mail system's evidence, profiles or action policy.

# One Message Is Not a Trusted Sender Baseline

A sender profile and a single message are different entities.

For the first integration, each immutable assessment gets its own subject. We rank tasks for this assessment without averaging its semantic readings into a sender's trusted historical profile.

That matters because a message being evaluated may itself be malicious. Feeding it straight into the baseline used to judge that sender can make the suspicious behaviour appear more ordinary over time.

StyloMail continues to own its trusted update rules and audit history. The bridge stores numeric readings and playbook metadata, while the original evidence retains its timestamp, provenance and coverage notes in StyloMail.

A stable assessment ID supports retries. Reusing it with changed coordinates is rejected; a revised assessment needs its own ID. Retention still bounds saved state, and a deployment must bound the number of tenant/representation engines it keeps alive.

# Run the Bridge Without a Model Call

The sample references the actual `StyloMail.Core` project. It does not duplicate the mail DTOs or require a live model to test the integration.

With `stylomail` and `mostlylucidweb` checked out beside each other, run from the web repository:

```sh
dotnet run --project samples/RelevanceMail/RelevanceMail.csproj
dotnet test samples/RelevanceMail.Tests/RelevanceMail.Tests.csproj
```

For a different layout, supply `-p:StyloMailSourceRoot=/path/to/stylomail`.

The console feeds deterministic fixtures through the adapter. Its output is:

```text
supplier-change: verify-supplier; policy stays Review
credential-request: check-credentials; policy stays Review
urgent-payment: urgent-payment; policy stays Review
missing urgency answer: insufficient_evidence; no recommendations=True
```

Those results verify the projection and matching example. They do not measure classification accuracy or demonstrate improved mail outcomes. The [sample README](https://github.com/scottgal/mostlylucidweb/blob/main/samples/RelevanceMail/README.md) describes the ownership, versioning and completion contracts.

Tests exercise evidence ordering, policy eligibility, valid zeroes, missing and duplicate answers, partial reads, stale results, producer mismatches and changed rubrics. The general Relevance solution remains independent of a StyloMail checkout; this cross-project example has its own test project.

# From Defined Questions to Useful Recommendations

We can now define a semantic dimension without training a bespoke classifier for it, retain the model's reading as evidence, and give the resulting vector a precise identity.

That is the bridge between the two parts of the series. Part 1 asks which relationships our representation should preserve. These questions give us another way to construct that representation. [Part 2](/blog/show-me-what-you-got-part2-building-relevance) explains how the engine uses it to retrieve eligible candidates, combine rankings and adapt to new observations.

Later, outcome evidence can tell us whether the recommended playbook helped. That belongs to Part 3's intervention-learning question, alongside recorded exposure and comparison with eligible alternatives.

The first step is already useful: ask a bounded question, keep the meaning of its answer, and let the rest of the system reason from that evidence.
