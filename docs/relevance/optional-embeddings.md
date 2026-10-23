# Optional local embeddings

Research date: 2026-10-23. These are implementation choices for the optional adapter, not claims that the prototype already ships either runtime or model weights.

Choose **all-MiniLM-L6-v2 through ONNX Runtime for the smallest initial text demo**, and qualify **EmbeddingGemma 2 through LLamaSharp for the richer multilingual and image path**. Neither becomes a dependency of `Mostlylucid.Relevance`. This is a fit recommendation, pending measurements on our workloads, rather than a universal model ranking.

## The shortlist

| Model | Footprint and output | Fit here |
|-------|----------------------|----------|
| `sentence-transformers/all-MiniLM-L6-v2` | 22.7M parameters, 384 dimensions, English; the published AVX2 quantised ONNX file is about 23 MB. | First lightweight text adapter. Short product descriptions and log messages, with a modest download. |
| `BAAI/bge-small-en-v1.5` | 33.4M parameters, 384 dimensions, 512-token context, English. | Retrieval-focused challenger in the same small-model evaluation. |
| EmbeddingGemma 1 | 308M parameters, multilingual text, 768 dimensions with supported truncation. | Existing text-only alternative; less compelling as the long-term series choice now that version 2 exists. |
| `google/embeddinggemma-2` | Modular: 270M text-only, 440M text plus vision, 740M with all encoders. Unified 768-dimensional output, supporting 512/256/128 truncation. | Preferred candidate for Part 4, where multilingual and cross-modal relevance justify the larger runtime. |

Sources: [MiniLM model card](https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2), [published quantised ONNX artifact](https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/blob/main/onnx/model_quint8_avx2.onnx), [Sentence Transformers runtime evaluation](https://www.sbert.net/docs/sentence_transformer/usage/efficiency.html), [BGE model card](https://huggingface.co/BAAI/bge-small-en-v1.5), [EmbeddingGemma versions](https://ai.google.dev/gemma/docs/embeddinggemma), [EmbeddingGemma 2 model card](https://ai.google.dev/gemma/docs/embeddinggemma/model_card_2).

The 23 MB figure is a model-file size, not process memory. That specific artifact targets AVX2; it is not the binary to assume works on this ARM64 Mac. Select and test an appropriate export for each architecture. ONNX Runtime, tokenizer data, working memory and caches add to the total. BGE's published benchmark advantage is a reason to evaluate it, not evidence of improved purchases or diagnostic decisions in our application.

## Is there a GGUF for EmbeddingGemma 2?

Yes. [Unsloth publishes an EmbeddingGemma 2 GGUF conversion](https://huggingface.co/unsloth/embeddinggemma-2-GGUF), including a roughly 310 MB Q8_0 text artifact. Companion modality assets and runtime memory are additional. Lower-bit variants exist; start with Q8_0 for qualification and measure any smaller variant against it.

[llama.cpp added the architecture on 6 October 2026](https://github.com/ggml-org/llama.cpp/pull/30054). A GGUF file alone does not establish LLamaSharp compatibility. LLamaSharp's [embedding example](https://github.com/SciSharp/LLamaSharp/blob/master/LLama.Examples/Examples/GetEmbeddings.cs) demonstrates pooled embeddings, and [v0.30.0 is the release to investigate](https://github.com/SciSharp/LLamaSharp/releases/tag/v0.30.0), but the exact backend package, native revision, projection output and modality bindings still need a load-and-infer test. We have not run that test and should not advertise support yet. Do not substitute arbitrary newer native libraries under incompatible managed bindings.

For product-to-product matching and clustering, use the documented symmetric task formatting consistently. A later natural-language search endpoint needs the separate query/document formatting. Truncated EmbeddingGemma vectors must be normalised again. Begin at 256 dimensions for evaluation, alongside the full output; 128 is an explicit quality tradeoff. The [model card](https://ai.google.dev/gemma/docs/embeddinggemma/model_card_2) documents these rules and warns against float16 activation inference. GGUF weight quantisation is a separate choice from activation precision.

## Keep integration small

The existing `IPointConverter` is the integration boundary. Proposed separate packages are `Mostlylucid.Relevance.Embeddings.Onnx` and `Mostlylucid.Relevance.Embeddings.LLamaSharp`. Each owns its tokenizer, pooling, projection, normalisation and runtime lifecycle. Application code registers an adapter once; YAML selects its registered name. These package names are planned, not currently published or implemented.

Illustrative future configuration, after registering `text-local`:

```yaml
points:
  meaning:
    path: description
    converter: text-local
    metric: cosine
    displays: Product description meaning
```

Model path, expected checksum, tokenizer revision and runtime profile belong in host configuration. YAML cannot download or execute arbitrary converters. Converter `Version` must fingerprint model/tokenizer hashes, formatting, pooling, dimension and normalisation. Two models producing 384 numbers still occupy different spaces. Changing a representation requires replay under a new engine identity in the current prototype.

For integration today, register an application-owned `IPointConverter`, or compute vectors outside the engine and use its existing `vector` converter with declared dimensions. The example above is deliberately not enabled in the runnable demo.

Encode candidate descriptions once when candidates change. Product interactions already reference those stored vectors, so a view or basket event does not invoke the encoder again. Cache raw-log encodings with bounded retention and keys including representation version. Never reuse only the message template when changing values carry the meaning. Backpressure remains visible: the current single worker awaits conversion, so uncached inference can delay other work. Benchmark that before introducing a separate bounded encoding queue; avoid detached work with uncertain save semantics.

## Qualification before enabling a default

Compare the numeric/category/colour baseline against baseline plus text, using held-out product matches and diagnostic relevance judgments. Then measure downstream recommendation quality separately. Record cold start, resident memory, model bytes, p50/p95 latency, throughput and event-to-read delay on ARM64 and x64. Evaluate quantisation with the same examples, especially negation, identifiers, short ambiguous terms, and multilingual inputs where claimed.

Verify tokenizer and pooling parity against the reference implementation, finite values, expected dimensions and cosine rankings. Measure long-input truncation explicitly. Pin artifact revisions/checksums and retain licence information; no implicit first-request model download. Our older SegmentCommerce ONNX service is useful prior art, but its simplified tokenizer should not be copied as a reference-correct implementation.

The next model-specific implementation gate is a real local inference smoke test followed by this evaluation. The completed provider/engine tests do not qualify an embedding runtime.

Images and shared-space matching continue in the [Part 4 plan](part4-richer-representations.md).
