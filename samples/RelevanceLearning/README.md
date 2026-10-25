# Part 4: email interventions

Run the actual app with `dotnet run --project Mostlylucid.Relevance.Demo`, then open
`http://127.0.0.1:5098/experiments.html`. The shop also links to the email lab.
No real mail is sent. Delivery, preview and link buttons exercise explicit first-party demo evidence.

The app detects contexts from recorded shop views, comparisons and budget changes.
It parses actual HTML into link occurrences, source text positions, regions and structural vectors.
Human edits and local-model rewrites become immutable new candidates in the existing Relevance engine.
The separate Learning library records assignments, propensities, stages and observed margin rewards.

Run the reproducible population with a new output directory:

```sh
dotnet run --project samples/RelevanceLearning -- --output /tmp/relevance-email-814
```

This creates 48 actual emails, 2,400 training visits and 1,200 later policy-evaluation visits.
The detector receives numeric observations, not the simulator's hidden audience labels.
A distinct random stream chooses assignments. Endpoint telemetry can be missing; those rows are censored.
People with no observed view remain in the assignment-level analysis when their endpoint is observed.
The adaptive policy continues updating during evaluation; this is not frozen-model evaluation.

Output includes `emails.json`, `episodes.json`, `snapshot.json`, `output.txt` and two persistent SQLite stores.
[Checked-in artifacts](artifacts/seed-814) include the article's measured output and representative evidence.
The reported policy comparison uses the lexical baseline; it does not claim that a model improved conversion.

## Optional Ollama producers

Use maxo for model calls. The client honours `OLLAMA_HOST` and otherwise defaults to `http://maxo:11434`.
SSH access is available through `claude@maxo`; if Ollama only listens on loopback, forward it:

```sh
ssh -N -L 11435:127.0.0.1:11434 claude@maxo
export OLLAMA_HOST=http://127.0.0.1:11435
```

Capture real typed decision readings and use them in a separately versioned experiment:

```sh
dotnet run --project samples/RelevanceLearning -- analyse --model nimble --output /tmp/email-characterisations
dotnet run --project samples/RelevanceLearning -- --output /tmp/email-model-run --characterisations /tmp/email-characterisations/characterisations.json
```

The checked-in model digest/readings and small-model rewrite were captured before switching hosts;
they are retained as actual prior outputs, not relabelled as a maxo run. No model downloads occur automatically.

The app's optional rewrite uses `Demo:RewriteModel` (default `qwen3.5:0.8b`), checks destinations/prices,
and requires a separate human review/register action. Its decision inspection uses `Demo:DecisionModel`
(default `nimble`). Inspection readings are not silently mixed into the lexical experiment's vector space.
Use the frozen-characterisation console path for a complete separately versioned model experiment.

## Boundaries

Text positions describe source order, not a rendered email-client viewport. Primary CTA identification is
an inspectable heuristic. Pixel activity is not proof of readership. A positive-content similarity profile
is not a treatment effect: assignment-level rewards and the controlled policy comparison remain separate.
The effect model is a small per-detected-group ridge baseline with bounded support and explicit exploration.
Its scores are expected normalized reward, not calibrated purchase probabilities.

Learning uses a persistent outbox for positive-response observations into Relevance. Outcome statistics commit
before that separate affinity update; retries drain the outbox idempotently. `RankAsync` avoids retaining another
core recommendation ledger when the learning layer already owns the exact assignment.
