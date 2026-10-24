# Part 5: Richer representations

Planned follow-up to **Understand the data → Understand the behaviour → Change the outcome**. The bells and whistles should demonstrate what new evidence buys us, with the same small save/recommend/predict surface.

## Start with the question

Show a shopper choosing a product from its photograph: can we recommend visually related products while retaining budget, availability and category constraints? Then show the diagnostic equivalent: a screenshot or chart can provide evidence alongside structured telemetry. Neither demonstration makes image similarity an outcome probability.

The [local embedding research](optional-embeddings.md) selects MiniLM as the first small text baseline and EmbeddingGemma 2 as the candidate for the multimodal extension. Actual runtime support and quality are qualification gates. Keep adapters optional and separately packaged.

## Ingestion

An image record supplies a candidate ID, stable asset reference, content hash and provenance. Host code resolves bytes through a bounded asset reader; model adapters handle decoding, orientation, resizing and model-specific preprocessing. Set explicit size/type limits. This is not arbitrary URL fetching from a converter.

Asset preparation and encoding run through bounded background work. Define acknowledgement precisely: either await the completed vector update as today, or expose an explicit pending status for a later ingestion mode. Never acknowledge a complete recommendation update while silently leaving an older image vector active. Store encoder/preprocessing versions and replace vectors atomically when an asset changes.

Repeated shopper events reuse the stored image vector. No model inference is needed for every click. Keep numeric state, category and measured colour points available alongside semantic representations.

## Two valid architectures

1. **Independent image head:** image-to-image neighbours form their own ranked list. Combine its ranking with text, price and behaviour through weighted RRF. The spaces need not be compatible because distances are never compared across them.
2. **Shared multimodal space:** use compatible encoders from one model/version to compare text and images directly. EmbeddingGemma 2 is a candidate for this. Validate the complete preprocessing, projection and normalisation path, including multimodal runtime bindings.

Equal dimensions do not make MiniLM text vectors comparable with EmbeddingGemma images. Upgrading to a multimodal model requires rebuilding the corresponding text vectors. Start with separate text/image observations for clear provenance before experimenting with joint image-plus-description encoding.

## What the reader should see

Add an optional visual head toggle and an image query to the existing shop. Show which head contributed each result, including where visual resemblance conflicts with function. Revisit russet: photograph similarity, a measured swatch and a word embedding answer different questions.

Keep cluster descriptions separate from evidence. A tiny language model can name a discovered community from bounded statistics and representative members, but its prose neither changes graph edges nor validates an explanation. Record cluster/model versions and show the supporting observations.

Part 4's controlled interventions remain responsible for testing whether choosing a product or image improves outcomes. Better-looking neighbours alone do not demonstrate that effect.

## Evidence for publication

Publish examples where visual information helps and fails, together with text-only and deterministic baselines. Measure retrieval quality, download size, memory, first-use and warm latency, indexing cost and filtered recommendation freshness. Include an offline run and model-disabled mode. Only call text/image search supported after real cross-modal inference succeeds on the packaged .NET backend.
