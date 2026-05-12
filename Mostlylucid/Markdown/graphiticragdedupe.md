# Deduplication of Graphitic RAG Evidence Segments in ***lucid*RAG**

<datetime class="hidden">2026-01-17T14:00</datetime>
<!-- category -- C#, lucidRAG, Vector Search, Machine Learning, Knowledge Graphs -->

> **NOTE:** This is not a conventional blog article. It is a **technical spec** written for a concrete feature in ***lucid*RAG**.
> I iteratively feed this document to code-focused LLMs during development to reason about trade-offs, validate assumptions, and converge on a rational implementation.

This document describes a subsystem from ***lucid*RAG**, a project I’m actively developing.

One core requirement of ***lucid*RAG** is the ability to extract **segments of evidence** - sentences, paragraphs, headings, captions, frames, or structured blocks - and ensure those segments are **deduplicated** without destroying useful signal.

***lucid*RAG** works by analysing and extracting the *best* available evidence from documents, images, audio, and structured data. Unlike most RAG implementations, it does **not** store LLM-generated summaries as the primary artefact. In many cases, ingestion requires no LLM at all (though one can be used when escalation is justified).

Instead, ***lucid*RAG** applies a wide range of deterministic and probabilistic techniques to:

* extract candidate segments
* evaluate their informational value
* and retain only the strongest representatives

**Deduplication is a critical part of this process.**

Simple string equality is not enough. The same concept is frequently expressed using different wording, structure, or modality. Treating those as distinct leads to redundant storage and poor downstream behaviour.

**The problem compounds at retrieval time.**

When results are retrieved (via SQL, vector embeddings, BM25, or hybrids), feeding an LLM multiple segments that all express the same underlying idea produces dull, repetitive answers. Five near-identical chunks from different documents do not add clarity - they dilute it.

To address this, ***lucid*RAG** treats deduplication as a **first-class compilation problem**, not a post-hoc filter.

The remainder of this document describes how that deduplication strategy was designed. This is part of how I work with Code LLMs, adapting my ['Agile Speccing'](https://www.mostlylucid.net/blog/writingfeaturespecs) adapted for the needs of Code LLMS. 

This crystalises the thoughts, different concepts and forces the LLM to document it's 'thought processes'. This is the first stage of ensuring the LLM *builds the right thing*. 


[See more about ***lucid*RAG** here. ](https://www.lucidrag.com)

# Deduplication Strategy

***lucid*RAG** uses a two-phase deduplication strategy to eliminate redundant content while preserving important signals. This is a **signal-preserving filter**, not content normalization.

## Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           INGESTION (Per Document)                          │
│                                                                             │
│  Document → Extract → Embed → DEDUPE (intra-doc) → Index to Vector Store   │
│                                   │                                         │
│                                   ├─ Near-duplicates: boost salience        │
│                                   └─ Exact duplicates: drop (no boost)      │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          RETRIEVAL (Cross Document)                         │
│                                                                             │
│  Query → Search → Rank (RRF) → DEDUPE (cross-doc) → Top K → LLM Synthesis  │
│                                   │                                         │
│                                   └─ Keep segment with highest RRF score    │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Design Guarantees

These invariants are maintained by the deduplication system:

| Guarantee | Description |
|-----------|-------------|
| **Ordering preserved** | Deduplication never changes semantic ordering after RRF ranking |
| **No concept loss** | Deduplication never removes all instances of a concept |
| **Document boundary respected** | Deduplication never crosses document boundaries at ingestion |
| **Content immutable** | Deduplication never alters embeddings or text content, only selection and salience scores |
| **Deterministic** | Given identical inputs and config, deduplication produces identical outputs |

---

## Non-Goals

This system explicitly does **not** attempt to:

- **Detect factual contradiction** - Two segments saying opposite things are not deduplicated
- **Canonicalize truth** - We don't pick a "correct" version across sources
- **Collapse paraphrases across documents at ingestion** - Each document keeps its own segments
- **Normalize terminology** - "ML" and "machine learning" in different docs are preserved separately
- **Replace entity resolution** - That's GraphRAG's job, operating at a different level

---

## Determinism & Reproducibility

Deduplication is fully deterministic:

- **Embeddings are immutable** once computed at extraction time
- **Sorting is stable** - segments with equal salience maintain original order
- **No randomness** - no sampling, no approximate ANN, no probabilistic thresholds
- **No external state** - dedup decisions depend only on the current segment set

**Why this matters:** Users debugging retrieval results can trust that re-running with the same inputs produces the same outputs. This aligns with ***lucid*RAG**'s broader "constrained fuzziness" philosophy - fuzzy matching with deterministic behavior.

---

## Why Two Phases?

### Phase 1: Ingestion Deduplication

**Goal:** Reduce storage and prevent intra-document redundancy.

**Problem:** Documents often contain repeated content:
- Boilerplate text (headers, footers, disclaimers)
- Copy-pasted sections
- The same concept explained multiple ways

**Solution:** Deduplicate within each document before indexing.

**Key Insight:** Near-duplicates are treated as *independent evidence of importance*, not redundancy. If an author explains a concept three different ways, that concept matters. We capture this signal by boosting salience.

### Phase 2: Retrieval Deduplication

**Goal:** Prevent the LLM from receiving redundant information across documents.

**Problem:** When querying across multiple documents, similar paragraphs may appear in different sources. The LLM shouldn't describe the same information multiple times.

**Solution:** After ranking by RRF (which combines semantic similarity, keyword match, salience, and freshness), deduplicate across documents keeping the highest-scoring segment.

**Why after RRF?** The RRF score represents the best holistic measure of relevance. Deduplicating before ranking would lose this signal.

### Why Salience Boost Only at Ingestion

The separation is intentional:

| Phase | What it captures |
|-------|------------------|
| **Ingestion boost** | Author emphasis - how much the document stresses a concept |
| **Retrieval score** | Query relevance - how well content matches user intent |

Mixing these at retrieval would entangle document intent with user intent. A concept repeated 5 times in a document is important *to that document*, but may not be relevant *to this query*. By boosting at ingestion, we preserve the author's signal without biasing query results.

---

## Phase 1: Ingestion Deduplication

### Location
`src/Mostlylucid.DocSummarizer.Core/Services/BertRagSummarizer.cs`

### Method
`DeduplicateSegments()`

### Algorithm

```
1. Filter segments by minimum salience threshold (0.05)
2. Sort by salience score (highest first)
3. For each segment:
   a. If no embedding → keep (can't compare)
   b. Check cosine similarity against all selected segments
   c. If similarity >= 0.90:
      - If same ContentHash → exact duplicate, drop silently
      - If different ContentHash → near-duplicate, boost kept segment's salience
   d. If no match → add to selected list
4. Apply salience boosts: +15% per near-duplicate merged
5. Cap salience at 1.0 to prevent any single concept from dominating
```

### Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `similarityThreshold` | 0.90 | Cosine similarity above which segments are considered duplicates |
| `salienceThreshold` | 0.05 | Minimum salience to consider (filters noise) |
| `boostPerNearDuplicate` | 0.15 | Salience boost per near-duplicate merged (+15%) |

### Examples

**Exact Duplicate (No Boost)**
```
Segment A: "Contact us at support@example.com" [hash: abc123]
Segment B: "Contact us at support@example.com" [hash: abc123]  ← same hash
Result: Keep A, drop B, no boost (likely boilerplate)
```

**Near-Duplicate (Boost Applied)**
```
Segment A: "Machine learning models require training data" [hash: abc123]
Segment B: "ML systems need data for training" [hash: def456]  ← different hash, 0.92 similarity
Segment C: "Training data is essential for ML" [hash: ghi789]  ← different hash, 0.91 similarity
Result: Keep A with +30% salience boost (concept emphasized 3 ways)
```

### Rationale for Thresholds

- **0.90 similarity:** Based on research (NVIDIA NeMo uses 0.90-0.92, industry standard for semantic dedup)
- **0.05 salience:** Filters very low-value segments while keeping most content
- **15% boost:** Meaningful signal without over-weighting repeated concepts

---

## Phase 2: Retrieval Deduplication

### Location
`src/LucidRAG.Core/Services/AgenticSearchService.cs`

### Method
`DeduplicateByEmbeddingPostRanking()`

### Algorithm

```
1. Receive ranked results (already sorted by RRF or dense score)
2. For each segment (in score order):
   a. If no embedding → keep
   b. Check cosine similarity against all selected segments
   c. If similarity >= 0.90 → skip (higher-scored duplicate already selected)
   d. If no match → add to selected list
3. Return deduplicated list (maintains score ordering)
```

### Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `similarityThreshold` | 0.90 | Cosine similarity threshold for cross-doc dedup |

### Why Post-RRF?

RRF (Reciprocal Rank Fusion) combines four signals:
1. **Dense score:** Semantic similarity to query
2. **BM25 score:** Lexical/keyword match
3. **Salience score:** Importance within document
4. **Freshness score:** Recency boost

Deduplicating AFTER RRF means we keep the segment that best matches the query across all dimensions, not just semantic similarity.

### Example

```
Query: "How do I configure authentication?"

Results before dedup:
1. [Doc A] "Authentication is configured via config.yaml..." (RRF: 0.052)
2. [Doc B] "Configure auth using the config.yaml file..." (RRF: 0.048, similarity to #1: 0.93)
3. [Doc A] "Set the API key in environment variables..." (RRF: 0.041)

Results after dedup:
1. [Doc A] "Authentication is configured via config.yaml..." (RRF: 0.052)
2. [Doc A] "Set the API key in environment variables..." (RRF: 0.041)

Doc B's similar paragraph dropped - Doc A's version had higher RRF score.
```

---

## Failure Modes & Trade-offs

### Known Limitations

| Failure Mode | Description | Mitigation |
|--------------|-------------|------------|
| **False positive (over-dedup)** | Two distinct but closely related concepts may exceed 0.90 similarity | Accepted trade-off favoring reduced redundancy; threshold is tunable |
| **False negative (under-dedup)** | Very short segments may embed poorly, missing semantic similarity | Hash-based exact duplicate detection catches identical text |
| **Embedding drift** | Changing embedding models invalidates dedup assumptions | Requires full re-ingestion; embeddings are immutable once stored |
| **Order sensitivity** | Greedy selection means first high-salience segment wins | Mitigated by stable sorting; highest-salience segment is kept |

### Accepted Trade-offs

- **Precision over recall:** We prefer occasionally keeping near-duplicates over accidentally removing distinct content
- **Storage over accuracy:** We store per-document rather than global dedup to preserve source attribution
- **Simplicity over optimization:** O(n²) is acceptable for typical document sizes; LSH adds complexity

---

## Multilingual Considerations

Deduplication behavior with multilingual content:

| Scenario | Behavior | Rationale |
|----------|----------|-----------|
| **Same language** | Normal dedup applies | Embeddings capture semantic similarity |
| **Cross-language paraphrases** | NOT deduplicated | Preserves source-language diversity |
| **Mixed-language document** | Dedup within language clusters | Embedding similarity naturally separates languages |

**Design choice:** Cross-lingual dedup is explicitly not supported. This preserves the ability to retrieve the same fact in the user's preferred language or compare how different sources phrase things.

Future enhancement: Cross-lingual dedup could be layered via translation-invariant embeddings if needed.

---

## Security & Adversarial Considerations

The deduplication system includes implicit protections:

| Attack Vector | Protection |
|---------------|------------|
| **Salience inflation via repetition** | Boost capped at 1.0; exact duplicates don't boost |
| **Copy-paste spam across documents** | Cross-doc dedup at retrieval removes redundant results |
| **Score manipulation via duplicate injection** | Dedup occurs AFTER ranking, preventing score inflation |
| **Boilerplate flooding** | Exact hash match detection drops without boost |

**Note:** Deduplication is not a security boundary. Malicious content that passes ingestion filters will be indexed. Content filtering should happen upstream.

---

## Interaction with GraphRAG

Deduplication and GraphRAG are intentionally orthogonal:

| System | Operates on | Purpose |
|--------|-------------|---------|
| **Deduplication** | Segments (text chunks) | Remove redundant retrieval results |
| **GraphRAG** | Entities & Relations | Build knowledge graph, resolve references |

**Why separate:**
- A segment mentioning "Apple" and another mentioning "the company" may dedupe as similar text but represent the same entity - that's GraphRAG's job to resolve
- Dedup doesn't need entity awareness; it operates purely on semantic similarity
- Entity-aware dedup could be added later as an enhancement, not a replacement

---

## Observability & Metrics

### Current Logging

Ingestion:
```
[dim]Deduplication: 150 → 98 segments[/]
```

Retrieval:
```
Post-ranking deduplication: 50 → 42 segments (removed 8 cross-doc duplicates)
```

### Recommended Metrics

For production monitoring, consider tracking:

| Metric | Description | Healthy Range |
|--------|-------------|---------------|
| `dedup_ratio_ingestion` | % segments removed at ingestion | 10-40% |
| `dedup_ratio_retrieval` | % segments removed at retrieval | 5-20% |
| `avg_salience_boost` | Average boost applied per document | 0.05-0.20 |
| `max_salience_boost` | Highest boost in a document | < 0.60 (else one concept dominates) |
| `dedup_by_doc_type` | Dedup rate segmented by document type | Varies |

### Debugging Tips

- **High ingestion dedup (>50%):** Document may have excessive boilerplate or be auto-generated
- **Low ingestion dedup (<5%):** Document has diverse content (good) or embeddings are poor (investigate)
- **High retrieval dedup (>30%):** Query may be too broad, or corpus has many similar documents
- **Salience approaching 1.0:** Concept was heavily emphasized; verify it's legitimate, not spam

---

## Configuration

Deduplication is configured via `DocSummarizerConfig.Deduplication` section in `appsettings.json`:

```json
{
  "DocSummarizer": {
    "Deduplication": {
      "Ingestion": {
        "Enabled": true,
        "SimilarityThreshold": 0.90,
        "SalienceThreshold": 0.05,
        "EnableSalienceBoost": true,
        "BoostPerNearDuplicate": 0.15,
        "MaxSalienceBoost": 1.0,
        "BoostDecayMode": "Logarithmic",
        "LogBase": 2.0
      },
      "Retrieval": {
        "Enabled": true,
        "SimilarityThreshold": 0.90,
        "MinRelevanceScore": 0.25
      },
      "Analytics": {
        "EnableLogging": true,
        "EnableMetrics": true,
        "HighIngestionDedupThreshold": 0.50,
        "HighRetrievalDedupThreshold": 0.30,
        "HighSalienceBoostThreshold": 0.60
      }
    }
  }
}
```

### Ingestion Configuration

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Enabled` | `true` | Enable/disable ingestion deduplication |
| `SimilarityThreshold` | `0.90` | Cosine similarity threshold for duplicate detection |
| `SalienceThreshold` | `0.05` | Minimum salience to consider (filters noise) |
| `EnableSalienceBoost` | `true` | Boost salience for near-duplicates |
| `BoostPerNearDuplicate` | `0.15` | Base boost per near-duplicate (+15%) |
| `MaxSalienceBoost` | `1.0` | Maximum salience cap |
| `BoostDecayMode` | `Logarithmic` | `Linear` or `Logarithmic` decay |
| `LogBase` | `2.0` | Base for logarithmic decay |

### Retrieval Configuration

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Enabled` | `true` | Enable/disable retrieval deduplication |
| `SimilarityThreshold` | `0.90` | Cosine similarity threshold |
| `MinRelevanceScore` | `0.25` | Minimum RRF score to include |

### Analytics Configuration

| Parameter | Default | Description |
|-----------|---------|-------------|
| `EnableLogging` | `true` | Log deduplication operations |
| `EnableMetrics` | `true` | Collect metrics for monitoring |
| `HighIngestionDedupThreshold` | `0.50` | Warn if >50% deduplicated at ingestion |
| `HighRetrievalDedupThreshold` | `0.30` | Warn if >30% deduplicated at retrieval |
| `HighSalienceBoostThreshold` | `0.60` | Warn if boost exceeds 60% |

### Boost Decay Modes

**Linear Mode** (simple, predictable):
```
boost = boostPerNearDuplicate × count
```
Example: 3 near-dupes × 0.15 = +45% boost

**Logarithmic Mode** (diminishing returns, default):
```
boost = boostPerNearDuplicate × log₂(1 + count)
```
Example: 3 near-dupes → 0.15 × log₂(4) = +30% boost

The logarithmic mode is recommended because:
- First few duplicates have the strongest signal (author emphasis)
- Many duplicates may indicate boilerplate, not importance
- Prevents runaway salience inflation

---

## Performance Considerations

### Complexity

| Phase | Complexity | Typical Size | Impact |
|-------|------------|--------------|--------|
| Ingestion | O(n²) | 50-500 segments | < 100ms |
| Retrieval | O(m²) | 20-100 segments | < 10ms |

### Scaling Path

For very large documents (10,000+ segments):

1. **LSH (Locality-Sensitive Hashing):** O(n) approximate dedup
2. **Batch comparison:** Process in chunks to reduce memory
3. **Early filtering:** More aggressive salience threshold

Current implementation is optimized for typical document sizes. LSH adds complexity without benefit for most use cases.

---

## Comparison with Research

| Approach | Threshold | Source |
|----------|-----------|--------|
| lucidRAG | 0.90 | This implementation |
| NVIDIA NeMo Curator | 0.90-0.92 | [SemDeDup docs](https://docs.nvidia.com/nemo/curator/latest/curate-text/process-data/deduplication/semdedup.html) |
| MinHash LSH (standard) | 0.80 Jaccard | [Google C4, GPT-3 paper](https://huggingface.co/blog/dedup) |
| SemHash | 0.90-0.95 | [GitHub](https://github.com/MinishLab/semhash) |

Our threshold of 0.90 aligns with industry best practices for semantic deduplication.

---

## What Is NOT Deduplicated

| Content Type | Reason |
|--------------|--------|
| Cross-document at ingestion | Preserves per-source resolution and attribution |
| Low-similarity content (<0.90) | Considered semantically distinct |
| Different segment types | Heading vs paragraph have different structural roles |
| Cross-language paraphrases | Preserves language diversity |

---

## Implementation Status

| Feature | Status | Location |
|---------|--------|----------|
| Configurable thresholds | ✅ Implemented | `DeduplicationConfig` class |
| Boost decay (log scale) | ✅ Implemented | `BoostDecayMode.Logarithmic` |
| Dedup analytics/metrics | ✅ Implemented | `DeduplicationResult<T>` record |
| DI service integration | ✅ Implemented | `IDeduplicationService` |

## Future Enhancements

1. **Dedup analytics dashboard:** Visual tracking of rates per document type
2. **Cross-lingual option:** Translation-invariant embeddings for multilingual dedup
3. **Entity-informed dedup:** Use GraphRAG entities as additional signal (not replacement)
4. **Prometheus/OpenTelemetry:** Export metrics for monitoring dashboards

---

## Summary

This deduplication strategy:

- **Preserves signal** - Near-duplicates boost importance rather than being discarded
- **Respects boundaries** - Documents maintain independent segment sets
- **Ranks then filters** - Uses full RRF signal before cross-doc dedup
- **Fails safely** - Prefers keeping content over aggressive removal
- **Stays deterministic** - Same inputs always produce same outputs