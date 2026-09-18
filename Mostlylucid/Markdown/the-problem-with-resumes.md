# lucidResume: The Problem With Resumes

<!--category-- AI, LLM, JobML, Résumés, RAG, Avalonia, Evidence, Open Formats -->

<datetime class="hidden">2026-09-18T12:00</datetime>

A résumé should work more like a scientific paper. The prose makes readable claims. Important claims link to the evidence which justifies them.

As I once more hunt for my next paying gig, I again face my nemesis: updating my résumé, CV, professional profile, or whatever we're calling the bloody thing this week. Two things annoy me; first, I have 35 years of professional experience and over 50 gigs (between FT and Contract) in that period. Some for 1 week, which were super interesting tech - I have a 3-month 'distributed computing for intelligence service' one, for example - but making a single document which covers ALL of the relevant experience while being human-readable AND machine-friendly is a task *I cannot successfully complete*, so...

A few months ago I made this odd research project [lucidResume](https://github.com/scottgal/lucidresume) which is a client based around the concept of a skills ledger based resume management system, now that I have to look for a job again I thought I'd put some thought into the problem...this is the early result of that research.

[![lucidRESUME release](https://img.shields.io/github/v/release/scottgal/lucidRESUME?filter=v*&label=lucidRESUME&logo=github)](https://github.com/scottgal/lucidRESUME/releases)
[![Mostlylucid.Avalonia.UITesting on NuGet](https://img.shields.io/nuget/v/Mostlylucid.Avalonia.UITesting.svg?logo=nuget&label=Avalonia%20UI%20Testing)](https://www.nuget.org/packages/Mostlylucid.Avalonia.UITesting)

I know there is a ruleset, but I cannot see it. The document is supposedly prose for a human, yet it needs the right structure, phrases and keywords to survive an ATS, recruiter search and whatever model has been bolted onto the process this month.

The modern résumé is an ODD document.

[TOC]

---

## The Adversarial Dance

The résumé is increasingly written by one AI and first read by another. It has to be parseable and keyword-complete, but natural enough not to look generated. You use AI because tailoring every application is ridiculous, then disguise that use in case another AI penalises it. You must be an individual, but also look sufficiently like everybody else who gets through the filter.

Only then might it reach a human, at which point it must suddenly become a concise and convincing career narrative.

So one document is being asked to do two different jobs:

1. Provide explicit, structured and searchable data for machines.
2. Provide selective, readable and persuasive prose for humans.

These are different loss functions. Optimising for one can make the document worse for the other.

## What Does the Research Actually Say?

There is plenty of research into hiring signals and discrimination, but surprisingly little experimental evidence for the ideal section order, page length, bullet count or keyword density. Most supposed rules are recruiters, résumé services and ATS vendors repeating advice to one another.

### Conventional Layouts Usually Win

[A controlled study by Arnulf, Tegner and Larssen](https://doi.org/10.1080/13594320902903613) found that formal layouts beat creative ones even when the candidate information was equivalent. It does not reveal the One True Template. It shows that presentation changes perception and predictable structure makes evidence easier to find.

### Detail, Clarity and Structure Matter

[A 2025 study by Wingate, Robie, Powell and Bourdage](https://doi.org/10.1111/ijsa.70022) followed 183 students through real applications. Clearer, more detailed and better-organised applications received more interviews and found work faster, even after controlling for experience, achievements and tailoring. The sample was narrow, but the useful conclusion is modest: compositional quality matters.

There is a wrinkle. If an LLM can improve clarity in seconds, writing quality may still influence selection while saying less about the candidate's own ability or effort. The signal survives as its meaning erodes.

### Recruiters Evaluate Signals

[Experimental studies](https://doi.org/10.1257/aer.20181714) show employers responding to visible signals such as experience, qualifications, grades and skills. The value of each changes by role and employer. [Eye-tracking research](https://doi.org/10.3389/fsoc.2024.1222850) also suggests selective scanning, although it does not justify the folklore that every recruiter reads a CV for exactly six or seven seconds.

All I think we can safely say is that recruiters inspect limited evidence and use it to infer qualification and fit.

The résumé is an information-retrieval interface pretending to be an essay.

### The Folklore Is Not Evidence

There is little strong independent evidence that:

* Every résumé must be exactly one or two pages.
* There is one universally optimal section order.
* Every achievement must use the same action-result formula.
* PDF always beats DOCX, or DOCX always beats PDF.
* A particular keyword density will satisfy an ATS.
* A professional summary always helps or always hurts.

ATS products are proprietary, varied and continually changing. The safest advice remains painfully generic: use predictable headings, preserve the reading order and make the evidence easy to recover. That is not optimisation. It is damage limitation.

## Two Loss Functions, One Document

Humans want selection, context and narrative: what did this person do, why did it matter, and do they sound credible? Machines want explicit entities, dates, relationships, skills and evidence.

Good prose compresses. It avoids repetition, relies on implication and varies its vocabulary. Good machine data does the opposite: explicit repetition, stable identifiers and predictable structure.

Trying to satisfy both produces keyword-stuffed prose followed by a disconnected technology list. That list can assert "distributed systems" without saying which project demonstrates it, what the candidate did, how recently or at what scale.

The prose and keyword list are manually maintained copies of the same information, so they drift. An edited role loses the only evidence for a skill. An application-specific keyword survives into later versions. Relevant experience never reaches the list. The document does not know these fragments refer to the same thing.

## Make the Résumé Behave Like a Paper

The useful analogy is not a database record. It is a scientific paper. Readable prose makes a claim. A citation lets the sceptical reader inspect the method, data or prior work which justifies it.

A résumé should be able to do the same thing:

```text
human claim
    ↓
inline evidence marker
    ↓
identified passage, project, repository, publication or qualification
    ↓
the reason this claim is allowed to exist
```

That does **not** make every statement scientifically proven. Evidence can be weak, stale or self-reported. The difference is that you can check it. "Kubernetes" stops being an orphaned keyword and becomes a claim linked to the work which supposedly demonstrates it.

The human résumé remains authoritative **about what its prose says**, not a complete career record. JobML is the richer evidence and citation layer embedded inside it. It may include external sources and support shorter role-specific renderings, but it must never overwrite the author's words or turn inference into fact.

## JobML: Citations for Résumé Claims

[JobML 0.1](https://github.com/scottgal/lucidRESUME/blob/main/docs/jobml-0.1.md) is the small format I built around that idea. An ordinary Markdown résumé is followed by a fenced YAML block. The Markdown is the authored document. The YAML records claims and points each one back to evidence in the prose or to an external source.

The rule at the centre of it is:

> **A JobML claim MUST be traceable to the human-readable or external evidence that supports it, and machine-derived information MUST NOT silently become evidential fact.**

Here is a complete miniature example:

````markdown
# Jane Smith

## Experience

### Example Corp {#example-corp}

Led the modernisation of a high-volume ASP.NET Core platform.

---

```jobml
jobml:
  version: "0.1"
  purpose: >
    Machine-readable representation of claims made by this resume. Claims are
    supported by human-readable prose or external evidence. Absence of a claim
    does not imply absence of a skill or capability.
  semantics:
    - Every substantive claim should be supported by one or more evidence references.
    - Do not infer unsupported claims or treat machine-derived suggestions as evidence.
    - Use both the human-readable prose and JobML when evaluating this resume.
    - Treat JobML as a higher-resolution description, not as replacement prose.

document:
  id: jane-smith-resume
  language: en-GB

entities:
  - id: example-corp
    type: experience
    name: Example Corp
    source: "#example-corp"

claims:
  - id: platform-modernisation
    subject: example-corp
    statement: Led modernisation of a high-volume ASP.NET Core platform.
    concepts:
      skills: [aspnet-core]
    supported_by:
      - type: prose
        ref: "#example-corp:p1"
        fingerprint:
          text: "fnv1a64:78cf6acd1fd870af"
        selector:
          type: TextQuoteSelector
          exact: Led the modernisation of a high-volume ASP.NET Core platform.
      - type: repository
        uri: https://github.com/example/platform

concepts:
  - id: aspnet-core
    type: skill
    name: ASP.NET Core
    aliases: [ASP.NET, .NET web development]
```
````

Read cold, its intent should be obvious. JobML is schema-checkable YAML, but the explanation travels inside the file. The `purpose` and `semantics` tell an unfamiliar LLM what the document means and what it must not infer.

A formal [JSON Schema](https://github.com/scottgal/lucidRESUME/blob/main/docs/jobml-0.1.schema.json) supports deterministic tools. There is also a *cold-parser test*: give the document to a general model with no JobML prompt and ask it to identify the claims, evidence and unsupported assertions. If it cannot work that out from the file, the format has failed.

## The Citation Must Survive Editing

An inline citation which silently points at the wrong paragraph is worse than no citation at all.

Paragraph numbers break when somebody inserts a paragraph. Exact text breaks when punctuation changes. Semantic similarity can suggest candidates, but must not reinterpret weaker prose as stronger evidence. If:

> Led the modernisation...

becomes:

> Contributed to the modernisation...

the old claim cannot remain green merely because the sentences occupy the same position and look semantically similar.

JobML uses three ways to find the cited passage:

1. A readable structural reference such as `#example-corp:p1`.
2. A fast FNV-1a 64-bit fingerprint of the normalised evidence text.
3. A [W3C Web Annotation-style `TextQuoteSelector`](https://www.w3.org/TR/annotation-model/#text-quote-selector) containing the exact quote and, where useful, its surrounding context.

The hash is non-cryptographic because it detects editing drift, not fraud. The reference says where the evidence should be. The fingerprint says whether it is still the reviewed text. The quote selector helps recover it after a move.

Reconciliation produces four honest states:

| State | Meaning |
|---|---|
| `valid` | The cited evidence resolves and still matches. |
| `changed` | The passage exists, but it is no longer the reviewed text, or it moved uniquely. |
| `missing` | The evidence can no longer be found. |
| `ambiguous` | More than one passage could be the citation target. |

Nothing is silently repaired. A unique move or edit can be offered for acceptance. Missing and ambiguous citations need a human decision. Accepted claims with unresolved evidence block publication.

You can reopen the file after editing and see exactly what JobML believed, why it believed it and what changed underneath it.

## Live Evidence, Not a Report You Run Later

The model must be visible while writing or it will drift like every skills list. The [lucidRESUME](https://github.com/scottgal/lucidRESUME) editor shows three things together:

* the editable Markdown + JobML source
* the human résumé preview
* live evidence cards and validation diagnostics

Selecting an evidence card jumps to its source span. Editing that span updates its state. Claims extracted during ingestion begin as `origin: derived` and `review: required`, and count for nothing until accepted. Publishing commits the Markdown, reviewed JobML and document fingerprint as one snapshot.

It works like following a citation in a paper. The prose stays readable and the justification is one click away.

## Different Roles Need Different Resolution

Authoritative does not mean exhaustive. A principal engineer résumé might retain the technical detail of an architecture migration. A CTO version might summarise the same evidence around risk and commercial outcome. Another application might omit it.

JobML can remain high-resolution while the prose is compressed. It can also expose a role-specific subset when the full graph would be irrelevant. The resolution changes. The evidence does not:

```text
reviewed evidence graph
       ├── detailed engineering résumé
       ├── compressed leadership résumé
       └── explicit machine-readable coverage for this role
                    ↓
             same cited evidence
```

You can summarise it. You cannot make it up. An LLM may offer a draft or sample,
but that is not the point of the system and it is not authoritative. The person
reviews, edits and owns the prose. Changed text is linked and reconciled with the
ledger before publication. Rendering does none of that work. It only projects
already reviewed ledger records into a role-specific document.

That split is the product. Human prose should remain natural writing for human
readers. JobML gives ATS and AI systems the explicit, high-resolution detail they
need, with citations. lucidRESUME is not an application bot and it is not trying
to make generated text pass as human text.

That distinction matters. The output pipeline is not a final prompt which asks a model to rediscover the candidate from source documents. Extraction happens once at ingestion, using deterministic parsing, NER, and optionally an LLM. Each result keeps its source, method, confidence, review state, and evidence fingerprint. The ledger is then kept current as sources change. Markdown, JobML, Word, and PDF are projections of a particular ledger revision. They are not new interpretations of it.

## Importing Several Imperfect Résumés

Most people begin with `resume-final.docx`, `resume-final-2.pdf`, an old LinkedIn export and three role-specific variants which disagree about dates and wording.

lucidRESUME imports multiple documents, parses experience, education, projects and skills, then presents merge candidates. Finding "Kubernetes" in two files gives us something to review. It does not establish a fact.

There are two ways this can go wrong:

* **structural uncertainty:** did the parser correctly recognise the role, date or paragraph?
* **evidential uncertainty:** does that paragraph actually justify the proposed claim?

Layout detection, deterministic parsing and a local model can improve the first. The second is a review decision. [LLamaSharp](https://github.com/SciSharp/LLamaSharp) and the local [grug-9b GGUF model](https://huggingface.co/ProCreations/grug-9b-gguf) assist extraction. Imported prose and explicit human acceptance are authoritative.

The Avalonia UI tests exercise import → merge → draft → reconcile → publish against multiple real DOCX variants. That matters more than a parser demo: dangerous failures occur between stages, when uncertain extraction quietly becomes accepted fact.

## Why Not Extend an Existing Résumé Format?

I looked at the obvious candidates before inventing another acronym:

| Format | What it is good at | Why it is not the authoring model |
|---|---|---|
| [JSON Resume](https://jsonresume.org/schema) | Portable structured résumé records and themes | Record-first, with no prose anchor, citation state or reconciliation lifecycle. It is a useful import/export target. |
| [HR Open Standards](https://schema.hropenstandards.org/) | Broad recruiting-system interchange | Deliberately much larger than an evidence-aware writing format. |
| [Europass](https://europass.europa.eu/system/files/2020-08/ECV_Schema_Documentation_v3.0.0_20200602.pdf) | Rich public-employment exchange | Useful downstream, but too broad and exchange-oriented for live authoring. |
| [h-resume](https://microformats.org/wiki/h-resume) | Human and machine content co-located in HTML | Skills remain assertions rather than evidence-linked claims with drift state. |
| [Schema.org Occupation](https://schema.org/Occupation) | Public linked data about people and occupations | A sensible web projection, not a document reconciliation model. |
| [W3C Web Annotation](https://www.w3.org/TR/annotation-model/) | Anchoring annotations to changing text | Exactly the useful citation machinery, but intentionally not a résumé schema. |

JobML does not replace these formats. It borrows the useful selector bits from Web Annotation and can export to JSON Resume, Europass or Schema.org when needed.

This is how I intend to avoid accidentally founding the W3C of CVs. 😄

## RAG, But With a Citation Discipline

[The DocSummarizer pipeline](/blog/docsummarizer-rag-pipeline) handles parsing, segmentation and citations before the LLM sees the document. [Reduced RAG](/blog/reduced-rag) narrows its input to useful signals and relevant evidence. [lucidRAG](/blog/lucidrag-multi-document-rag-web-app) keeps sentence-level links from answers back to their sources. [DoomSummarizer](/blog/doomsummarizer-deep-research#evidence-assignment-the-heart-of-reduced-rag) uses the same idea for deep research. Generated synthesis is not the source. The evidence is.

JobML applies that discipline to a far smaller, stranger document.

When comparing a résumé with a job description, the path should be:

```text
job requirement
      ↓
semantic concept
      ↓
reviewed candidate claim
      ↓
valid citation
      ↓
original human prose or external evidence
```

That allows a coverage report to say:

```text
ASP.NET Core          ✓ directly evidenced
Kubernetes            ✓ directly evidenced
SOC 2                 ? related evidence; review required
Terraform             ✗ no supporting evidence found
```

The last line is not permission for an LLM to sprinkle "Terraform" into the résumé. It means we found no evidence for it. So don't add it.

## What JobML 0.1 Deliberately Does Not Do

The format supports Markdown, embedded YAML, entities, claims, concepts, aliases, evidence, drift detection and requirement coverage. It does not standardise a universal skill ontology, mandate embeddings, attest cryptographically, verify employers or integrate every ATS.

It never silently rewrites prose. Ask for a draft or role-specific sample and it
can suggest wording. The result stays a draft until the author reviews, edits and
accepts it. It cannot become evidence merely because a model wrote it.

A paper is not valuable because every sentence is magically true. It is valuable because you can follow the important claims back to their justification, argue with them and revise them.

That is what I want from a résumé:

> **Do not merely tell me what this person claims. Let me follow the citation and see why the document is entitled to claim it.**

The résumé can return to good human prose. JobML handles explicit relationships, retrieval and verification without forcing that prose to impersonate a database.

That's the point: one document, as much or as little detail as the role needs, and a way back to the evidence for every important machine claim.

## References

* Arnulf, J. K., Tegner, L. and Larssen, Ø. (2010). ["Impression making by résumé layout: Its impact on the probability of being shortlisted."](https://doi.org/10.1080/13594320902903613) *European Journal of Work and Organizational Psychology*, 19(2), 221-230.
* Kessler, J. B., Low, C. and Sullivan, C. D. (2019). ["Incentivized Resume Rating: Eliciting Employer Preferences without Deception."](https://doi.org/10.1257/aer.20181714) *American Economic Review*, 109(11), 3713-3744.
* Piopiunik, M., Schwerdt, G., Simon, L. and Woessmann, L. (2020). ["Skills, signals, and employability: An experimental investigation."](https://doi.org/10.1016/j.euroecorev.2020.103374) *European Economic Review*, 123.
* Törngren, S. O., Schütze, C., Van Belle, E. and Nyström, M. (2024). ["We choose this CV because we choose diversity: What do eye movements say about the choices recruiters make?"](https://doi.org/10.3389/fsoc.2024.1222850) *Frontiers in Sociology*, 9.
* Wingate, T. G., Robie, C., Powell, D. M. and Bourdage, J. S. (2025). ["The Signals That Matter: Resumes, Cover Letters, and Success on the Job Search."](https://doi.org/10.1111/ijsa.70022) *International Journal of Selection and Assessment*, 33(3).
