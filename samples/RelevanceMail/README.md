# StyloMail question vectors → Relevance playbooks

An optional composition example using **the real StyloMail.Core types**, not copied DTOs. It consumes existing `SemanticAssessment` / `Evidence` records and ranks host-defined review playbooks using the Relevance library.

No model request, credentials, mail transport or live mailbox is needed. The console fixtures exercise the integration; their results do not measure Jev/Nimble classification quality.

## Run

Check out [scottgal/stylomail](https://github.com/scottgal/stylomail) beside `mostlylucidweb`. This sample was checked against StyloMail commit `d4d60cc77d22e112ac11fbbba83dee65161cb185`.

```sh
dotnet run --project samples/RelevanceMail/RelevanceMail.csproj
dotnet test samples/RelevanceMail.Tests/RelevanceMail.Tests.csproj
```

For another checkout location, supply `-p:StyloMailSourceRoot=/path/to/stylomail`. The sample deliberately lives outside `Mostlylucid.Relevance.slnx` so the general library and tests remain buildable without a StyloMail checkout.

Expected first choices: `verify-supplier`, `check-credentials`, `urgent-payment`. The incomplete assessment explicitly abstains. SQLite storage defaults to a fresh temporary file; pass a database filename after `--` to reuse one.

## Existing StyloMail composition point

After classification and **after the existing versioned policy decides**:

```csharp
SemanticAssessment semantic = await classifier.ClassifyAsync(input, cancellationToken);
// Existing StyloMail policy combines semantic and deterministic evidence here.
MailAction authorisedAction = policyDecision.Action;

MailRecommendation advice = await recommendations.RecommendAsync(
    assessmentId, authorisedAction, semantic,
    actualQuestionSchemaVersion, cancellationToken);
```

This is a composition sketch: `policyDecision` comes from the existing policy pipeline; no new API is invented for that pipeline. Construct one `MailRecommendations` instance per tenant/representation using trusted question definitions, actual schema/preprocessing versions and the exact expected `Evidence.SourceVersion`. Upsert host-owned prototypes with `SaveAsync`. The wrapper owns its engine/store and worker, and should be disposed with `await using` at host shutdown after callers finish.

A singleton instance per tenant/schema is enough for this sample. Do not create an unbounded instance registry. A larger host can introduce an explicit bounded tenant lifecycle and shared worker integration separately.

`MailRecommendations` is **an executable integration component, not installed in StyloMail's live host**. StyloMail currently retains its own profiles and policy; this sample does not replace them. Relevance is a candidate dependency for its advisory core after integration and workload evaluation.

## Contracts that matter

- Questions are ordered and versioned. Their complete rubrics, order, expected producer, preprocessing and tenant identity fingerprint the engine scope.
- Existing evidence is selected by stable signal ID, not response enumeration order. Origin, availability, finite [0,1] values, producer and reason-tagged partial reads are checked. Duplicate selected semantic rows abstain.
- The wrapper accepts complete observations only. Relevance's current dense-vector path cannot implement StyloMail's masked comparisons; missing/reduced/partial/stale evidence yields no suggestions rather than zero-filled vectors or a cold-start list.
- A numeric zero is valid evidence. A complete value near 0.5 is an uncertain yes/no judgement, not a missing answer or measured severity. Completeness does not establish certainty or calibration.
- L2 retains magnitude. Do not unit-normalize independent yes/no scores indiscriminately.
- Ranking is relative to eligible prototypes. This sample has no absolute distance threshold for a good match; complete evidence can still produce weak matches. Inspect distances and evaluate a rejection policy before treating any suggested task as appropriate.
- The trusted policy action is a hard eligibility filter. Returned IDs name review tasks, never `MailAction` replacements. Nothing executes a task or forwards mail.
- Each immutable assessment has its own subject. This avoids averaging untrusted mail into a sender's trusted history. Same-ID/same-vector retries deduplicate; revised evidence needs a new assessment ID.
- Original timestamps, model evidence and coverage notes remain in StyloMail's audit ledger. The bridge persists no subject/body, and its result carries the representation fingerprint for correlation.
- Save and recommend are separate awaited operations. Cancellation after saving does not roll back that save; retry with the stable assessment ID.
- Outcomes are not learned here. Record actual exposure and final outcomes before adding the Part 3 intervention extension; rank scores and distances are not fraud probabilities.
