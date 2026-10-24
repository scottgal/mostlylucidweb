# SHOW ME WHAT YOU GOT (Part 1.5): Vectorise Anything With Decision Models

“How do I vectorise this?”

It is a reasonable question after [Part 1](/blog/show-me-what-you-got-streaming-recommendations). We know how useful vectors can be. We can compare entities, maintain centroids, discover groups and track change. But where do the coordinates come from when the thing we care about is difficult to measure?

Take a voice message. **Is this message aggressive?**

The transcript might say “That is absolutely fine.” The recording might sound reassuring, sarcastic or threatening. Counting words misses something. Measuring volume helps describe the signal, but a loud recording is not necessarily aggressive. We want a representation that captures the property relevant to our application.

Decision models provide a useful new answer: **define the dimensions as questions, then encode the answers**. With a suitable multimodal model, the evidence can be text, images or sound.

I think of this as **enabling embeddings for almost anything**. More precisely, it gives us a way to build task-specific semantic vectors over things a model can interpret, using dimensions whose meanings we choose.

The difficult feature-extraction problem becomes a question-design problem that we can inspect, version and test. It still requires judgement, but we have a practical way to begin without training a separate classifier for every property.

<datetime class="hidden">2026-10-23T15:00</datetime>
<!-- category -- AI,Architecture,Machine Learning,Behavioural Inference,Recommendations,Decision Models,Multimodal,C# -->

[TOC]

# Start with the Relationship You Need

A vector is useful because its coordinates preserve relationships relevant to a decision. The first question is therefore what we want to compare.

Suppose we are building a support system. We might want to recognise requests that need a human, find earlier cases with similar communication patterns, or identify conversations whose delivery is becoming hostile.

Those are related tasks, but they need different properties. “These clips contain similar words” is different from “these speakers use similar delivery”.

For a voice message, we could begin with these dimensions:

| Dimension | Question | Evidence needed |
|-----------|----------|-----------------|
| Aggressive delivery | Does the speaker use hostile or threatening delivery? | The recording, with a defined rubric |
| Explicit threat | Does the speaker express a threat in the spoken content? | Spoken words and relevant context |
| Request for a person | Does the speaker ask to speak with a human? | Spoken content |

We could add shouting, frustration or sarcasm as separate dimensions when useful. They should not all disappear inside a single “bad customer” score.

The entity is the message, or a defined segment of it. The coordinates are readings of these properties. What the system subsequently does with them is a separate decision.

> **Modelling aside: choose a property, not an entire verdict.** “Does this clip contain an explicit threat?” gives a dimension a clear job. “Is this customer a problem?” bundles several interpretations together and gives us much less control over the resulting representation.

# Why This Used to Be Awkward

If a useful property is already measured, vectorising it is straightforward. A latency, price or count can become a coordinate after we define its units and scaling.

Language and perception introduce more work. Recognising an aggressive delivery pattern can involve wording, intonation, stress, timing and context. An image property such as “the package is visibly crushed” also needs interpretation rather than a direct database lookup.

We could build rules, extract specialised features, collect labelled examples and train a classifier. Those remain useful approaches. A reusable decision model adds another route: describe the distinction in a question and supply the evidence it needs to judge it.

That changes the cost of trying a dimension. We can test several well-defined questions against a small evaluation set before committing to a specialist training pipeline.

The model does not discover the application's objective for us. We still choose which differences should matter.

# From LucidRAG Signals to Question-Defined Features

This is a continuation of the work in [DocSummarizer](/blog/building-a-document-summarizer-with-rag) and [lucidRAG](/blog/lucidrag-multi-document-rag-web-app). Those articles build useful representations by combining extraction, specialist models and several kinds of analysis. The application turns their outputs into structured signals that retrieval and reasoning can use.

The modality-specific summarisers make that approach especially clear:

| Earlier implementation | How it characterises the input |
|------------------------|--------------------------------|
| [ImageSummarizer](/blog/constrained-fuzzy-image-intelligence) | Combines measured colour and quality, OCR, image classification, semantic embeddings and optional captioning |
| [AudioSummarizer](/blog/audiosummarizer-forensic-audio-characterization) | Combines acoustic measurements, transcription, speaker diarisation and voice embeddings in a signal ledger |
| [DataSummarizer](/blog/datasummarizer-how-it-works) | Computes schema and distribution profiles, quality signals and drift, with model-assisted interpretation |

These pipelines do more than run one embedding model. Different analytics expose different properties, and code gives those outputs a consistent meaning. Building a new semantic dimension can mean selecting components, combining their evidence and implementing the interpretation that sits above them.

For the voice example, that could involve transcript analysis alongside acoustic features, speaker segmentation and a classifier interpreting the combination. Loudness is available as a measurement; deciding whether a particular delivery sounds hostile is another layer.

**Decision models simplify that semantic characterisation step.** With a capable audio model, we can define the hostility question and its rubric, supply the relevant recording, and consume a typed reading. We have moved some of the work from constructing a custom analysis pipeline to specifying and evaluating a question.

```text
Specialist analytics + ML models → application combines signals → semantic feature

Evidence + defined questions → decision-model readings → semantic feature vector
```

The architecture still benefits from the earlier [Reduced RAG](/blog/reduced-rag) principle: characterise the source once, retain structured evidence, and reuse it. Measured acoustic properties, exact statistics, OCR text and specialist embeddings remain useful contributors. Decision questions give us a simpler producer for suitable semantic properties alongside them.

That is the progression: the summarisers show how much useful characterisation we can assemble; decision models make more of those dimensions accessible through a common question-and-answer interface.

# A Vectoriser Built from Classifiers

A decision model accepts a situation and defined possible answers, then scores those answers. Its output is designed for code to consume.

That is classification. “Is the delivery aggressive?” distinguishes two classes: the condition is present or absent. “Which team should handle this?” distinguishes several categories. The useful move here is to retain the answer scores as coordinates, rather than keeping only the winning label.

Our question-based vectoriser is therefore a **bank of classifiers**. Each question supplies a defined distinction; the decision model provides a reusable way to evaluate it against the input. We can ask several questions of the same recording and collect their outputs:

```text
recording → [aggressive? explicit threat? asks for human?]
          → [       0.88,            0.07,           0.96]
          → a vector for comparison, grouping or recommendation
```

These are illustrative scores. The three conditions can coexist: this is several classification readings over one input, rather than one mutually exclusive label for the whole message.

Classification supplies features; the subsequent comparison or ranking uses them. A numeric conversion or general embedding encoder can also produce features without assigning named classes. Vectorisation is the broader operation.

For text, [Jev](https://docs.typesafe.ai/introduction) and [Nimble](https://ollama.com/library/nimble) expose typed questions. Their familiar shapes include yes/no judgements, choices among categories and positions on an ordered rubric.

Those shapes give us different kinds of coordinates:

| Question shape | Example | Possible representation |
|----------------|---------|-------------------------|
| Yes/no | Is an explicit threat present? | Model-assigned probability of yes |
| Choice | Which kind of request is this? | An ordered vector of category probabilities |
| Ordered score | How strong is the hostile delivery under this rubric? | A defined rubric position, with its interpretation retained |

For a yes/no question, the TypeSafe name is **Noul**. Its answer is a value in `[0,1]` representing the probability of yes; it does not include a separate confidence field. [The Noul contract](https://docs.typesafe.ai/primitives/noul) explains that distinction.

That probability is about the proposition we asked. “Aggressive delivery is present” is not the same proposition as “this person intends harm”.

> **Probability aside: presence is not intensity.** A Noul answer near 0.5 means the model gives yes and no similar probability. It does not mean “moderately aggressive”. For intensity, define levels such as calm, firm, hostile and threatening, and decide what those levels actually describe.

For an unordered choice, do not turn category IDs into an invented numerical scale. Retain a one-hot result or the full probability distribution in a fixed category order. For an ordered score, any numerical spacing or normalisation is also a modelling choice.

A general multimodal model can produce structured feature judgements too. A generated JSON field such as `"confidence": 0.93` is not automatically equivalent to a decision model's answer distribution. Preserve how each producer obtained its values.

# Voice Needs the Voice

Return to “That is absolutely fine.”

If we only supply a transcript, we have asked a model to interpret words. It cannot recover the actual delivery from text that discarded it. A transcript pipeline can help with semantic content; audio-native interpretation is the route for dimensions that depend on how something sounds.

This is becoming practical with newer models:

- [Clef and Clef Flash](https://docs.ollama.com/capabilities/decision) accept images alongside text for typed decisions.
- [Liquid AI's d1-omni](https://www.liquid.ai/blog/d1-open) accepts text with images or audio. Liquid describes the audio-capable checkpoint as experimental.
- [Gemini's audio documentation](https://ai.google.dev/gemini-api/docs/audio?authuser=0) includes answering questions about recordings and emotion detection, with a structured-output example.

These establish the capability to ask questions over the modality. They are not interchangeable implementations or proof that a particular aggression rubric works on our recordings.

For example, the documented [d1-omni Ollama integration](https://ollama.com/library/d1-omni) supports short English speech clips. Its published notes emphasise requests and speaker intent, rather than validated aggression detection. We can use that interface to explore audio questions while choosing and evaluating an appropriate model for the actual property.

Here is an illustrative request shape for a voice-delivery question:

```json
{
  "model": "d1-omni",
  "state": "Assess the audible speaker delivery in this recording.",
  "images": ["<base64-encoded WAV clip>"],
  "questions": {
    "aggressive_delivery": {
      "type": "noul",
      "instructions": "Does the speaker use hostile or threatening delivery in this clip?",
      "criteria": {
        "true": "The delivery conveys hostility or intimidation under the evaluation rubric.",
        "false": "The delivery is calm, neutral or firm without hostility or intimidation."
      }
    }
  }
}
```

`images` is the documented field used for the WAV payload by that particular integration; it is not a general audio API convention. This is a request example, not a measured aggression-classification result.

The important separation is between the supplied recording and our description of the property. The model produces a reading; we retain its meaning and provenance as a feature.

If the clip is inaudible, contains several speakers we cannot distinguish, or lacks the context required by the question, that limitation must survive the conversion. Shouting at a football match and threatening a colleague need not mean the same thing under our rubric.

# Images Use the Same Pattern

For a product photograph, the dimensions might be:

```text
[visible shoulder straps, visible wheels, visible package damage]
```

For an interface screenshot, they might be:

```text
[error dialog present, chart visible, sign-in prompt present]
```

The same question contract applies, with a vision-capable producer supplying the readings. Clef's [documented image API](https://ollama.com/library/clef) receives encoded images shared by the questions.

Ask about what the evidence can support. A photo may show a torn package. It cannot establish whether an unseen item works electrically. A description may claim waterproofing, while a photo shows only the shape of the bag.

This extends the russet example from Part 1. Different representations preserve different interpretations. We can now define semantic dimensions over the source itself, rather than hoping a general embedding has made exactly the distinction our decision requires.

# Turn the Readings into a Vector

Suppose our voice questions return these illustrative values:

```text
aggressive_delivery  0.88
explicit_threat      0.07
asks_for_human       0.96
```

We fix the coordinate order:

```csharp
var dimensions = new[]
{
    "aggressive_delivery",
    "explicit_threat",
    "asks_for_human"
};
```

The resulting representation is:

```text
voice message = [0.88, 0.07, 0.96]
```

Those teaching values describe strong model-assigned evidence for aggressive delivery and a request for a human, with little evidence for an explicit threat. They are not observed results from a recording.

Each question concerns a property that can coexist with the others. The three numbers do not need to sum to one. Separate questions also need not be statistically independent: correlated properties can give the same evidence extra influence if we weight them carelessly.

Building the array is easy. Ensuring that every position keeps the same meaning is the important part.

We need the ordered question definitions, the producer and model version, the units or answer type, and the preprocessing that selected the evidence. For audio, that includes segmentation: a five-second clip and a whole conversation are different observations.

# Is This an Embedding?

It is a representation of an input in a numerical space. Calling it an embedding is reasonable in that broad sense. The useful distinction is that this is a **question-defined semantic vector**: its axes have explicit meanings selected for our task.

A conventional embedding model learns a broad latent representation. We generally compare its vectors without assigning a human-readable property to each coordinate. Both approaches can be valuable.

| Representation | Good starting point when |
|----------------|--------------------------|
| Measured features | We already have the quantities we need |
| General learned embedding | We want broad similarity, retrieval or semantic neighbourhoods |
| Question-defined vector | We want comparisons along particular interpretable properties |
| Combination of contributors | Different aspects of the decision need different evidence |

We are not reversing an embedding model and uncovering its hidden aggression axis. We are defining an axis, then using a model to estimate the corresponding property from evidence.

That is why “enabling embeddings for almost anything” is interesting. The input no longer needs an obvious hand-coded feature extractor for every semantic property. A capable model supplies that interpretation, and the representation remains explicit enough to inspect.

“Almost” still matters. The property must be observable from the supplied evidence, the question must be coherent, and the producer must be capable enough for the use we have in mind.

# Missing Is Not Zero

A missing aggression answer is not a calm delivery. A missing image answer is not evidence that no damage exists.

Retain the distinction between:

| Reading | Meaning |
|---------|---------|
| Available value near zero | Model-assigned evidence for no |
| Available value near 0.5 | A balanced yes/no judgement |
| Unavailable | No usable reading was produced |
| Reduced coverage | A reading over incomplete evidence |
| Not applicable | The question does not apply to this observation |

We can exclude masked dimensions, require a minimum amount of compatible evidence, or abstain when essential readings are missing. Filling them with zero changes the observation.

> **Geometry aside: do not normalise away the property.** Two vectors may point in the same direction while assigning very different yes probabilities. Unit-length normalisation can erase that distinction. A distance over original values, with explicit weights and missing-value rules, can be a better starting hypothesis for these coordinates.

The question schema is also versioned evidence. If “aggressive delivery” changes from a hostility rubric to a volume threshold, the old and new coordinate no longer mean the same thing. Reusing the same ID does not preserve compatibility.

# A Small Existing Example: StyloMail

StyloMail already uses this pattern for text. It asks about credential requests, payment redirection and urgency, keeping each answer as semantic evidence.

One of its [actual question definitions](https://github.com/scottgal/stylomail/blob/d4d60cc77d22e112ac11fbbba83dee65161cb185/src/StyloMail.Core/SemanticDimension.cs) contains:

```csharp
Id = "semantic.payment_redirection",
Instructions = "Does this message attempt to introduce or change payment destination details?",
CriteriaTrue = "Supplies or changes bank account, routing, invoice or payment destination details.",
CriteriaFalse = "No introduction or change of payment destination.",
```

That is the same mechanism as the voice example, with a different source and different dimensions. A legitimate supplier change can score highly without becoming a fraud verdict.

Its existing [`EvidenceVectors.Semantic`](https://github.com/scottgal/stylomail/blob/d4d60cc77d22e112ac11fbbba83dee65161cb185/src/StyloMail.Assessment/EvidenceVectors.cs) conversion retains availability and masks rather than fabricating zeroes. The [Jev](/blog/stylomail-behavioural-inference-with-jev) and [Nimble](/blog/stylomail-using-nimble-from-csharp) articles cover the provider calls.

The runnable [RelevanceMail sample](https://github.com/scottgal/mostlylucidweb/tree/main/samples/RelevanceMail) reuses those real evidence types, projects selected questions in a stable order and ranks eligible review tasks. Its strict dense-vector path abstains on incomplete input. It demonstrates the connection to the recommendation engine; it does not implement the audio or image examples in this article.

# Test the Dimension Before Depending on It

For voice aggression, a useful evaluation set should include clips with similar words and different delivery, loud but friendly speech, quiet hostility, explicit threats and firm complaints without threats.

That is more revealing than testing only obviously angry recordings. We want to know whether the coordinate captures the distinction we defined.

Compare audio input with transcripts alone when testing a delivery-dependent dimension. If the results do not change when the relevant vocal cues change, we have not demonstrated that the model is using them usefully.

The image equivalent compares visible damage with harmless folds, shadows and packaging designs. Question changes should be tested on held-out examples rather than repeatedly tuned against the same successful demonstration.

Then evaluate the downstream use separately. A readable axis and plausible scores do not establish that a recommendation, routing rule or escalation policy improves outcomes.

# From Almost Anything to Relevance

Once we have a compatible representation, the rest of the vector machinery becomes available: distance, summaries, neighbourhoods, clustering and change over time.

A support system could characterise successive voice messages along defined communication dimensions. A catalogue could describe photographs through visible properties. A diagnostic assistant could characterise reports by the conditions they describe. The input modalities differ; the downstream contracts can remain similar.

Compute the representation when the observation or asset changes, then reuse it for comparisons. A model need not reread the recording every time we ask for a recommendation.

[Part 2](/blog/show-me-what-you-got-part2-building-relevance) takes the next step: use the characterisation to retrieve eligible candidates, combine useful rankings and update the decision as new observations arrive. [Part 3](/blog/show-me-what-you-got-part3-predict) learns outcome forecasts from observed journeys. Part 4 adds experiments, and Part 5 builds out richer ingestion, including images.

The practical answer to “how do I vectorise X?” is to decide which relationships matter, define the dimensions that preserve them, and choose producers capable of supplying those readings. Decision models make semantic properties across more kinds of input accessible through that same small pattern: **question → reading → coordinate**.
