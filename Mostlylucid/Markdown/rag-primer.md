# RAG Explained: A Practical Primer on Retrieval-Augmented Generation

<datetime class="hidden">2025-01-21T09:00</datetime>
<!-- category -- AI, RAG, Machine Learning, Semantic Search, LLM, AI-Article -->

# Introduction

If you've been following the AI space, you've probably heard about RAG (Retrieval-Augmented Generation). It's one of those terms that gets thrown around constantly, but what does it actually mean? More importantly, how does it work, and when should you use it?

RAG is the technology that makes AI systems smarter by giving them access to relevant information at the moment they need it. Think of it like an open-book exam versus memorizing everything - the AI can look up specific facts rather than trying to remember everything it was ever trained on.

**Why does RAG matter?** Large Language Models (LLMs) are powerful, but they have fundamental limitations:
- They only "know" what they were trained on (their knowledge cutoff)
- They can hallucinate facts with confidence
- They can't access private or recent information
- Fine-tuning them is expensive and requires retraining for every update

RAG solves these problems elegantly by combining the reasoning power of LLMs with the precision of search. Instead of asking an LLM to generate an answer from memory alone, RAG first retrieves relevant information from a knowledge base, then feeds that context to the LLM to generate accurate, grounded responses.

In this two-part primer series, I'll explain what RAG is, where it came from, exactly how it works under the hood, and when you should (and shouldn't) use it. We'll use concrete C# code examples throughout to illustrate the concepts.

**In Part 1 (this article)**, we'll cover:
- What RAG is and its historical context
- How RAG works: indexing, retrieval, and generation
- Understanding LLM internals: tokens, KV cache, and context windows
- RAG vs. other approaches (fine-tuning, long context, prompting)

**In [Part 2](/blog/rag-primer-practical)**, we'll dive into practical applications:
- Real-world RAG implementations on this blog
- Common challenges and solutions
- Advanced techniques (HyDE, multi-query, contextual compression)
- Step-by-step guide to building your own RAG system

[TOC]

# What is RAG?

**Retrieval-Augmented Generation** is an AI technique that enhances language models by giving them access to external knowledge sources. Rather than relying solely on the model's training data (which is static and can become outdated), RAG systems dynamically retrieve relevant information and use it to inform the model's responses.

The core concept is simple:

```mermaid
flowchart LR
    A[User Question] --> B[Retrieve Relevant Info]
    B --> C[Retrieved Documents/Context]
    C --> D[Generate Response]
    A --> D
    D --> E[Grounded, Accurate Answer]

    style B fill:#f9f,stroke:#333,stroke-width:2px
    style D fill:#bbf,stroke:#333,stroke-width:2px
```

Instead of:
1. User asks question → LLM generates answer (potentially hallucinated)

RAG does:
1. User asks question → **Find relevant information** → LLM generates answer **using that information**

**Key insight:** The LLM doesn't need to memorize everything. It needs to be good at reasoning over the information you give it. This is why RAG is so powerful - it separates "knowledge storage" (the retrieval system) from "reasoning" (the LLM).

# Where Did RAG Come From?

RAG isn't brand new - it builds on decades of research in information retrieval and natural language processing.

## The Historical Context

**Traditional Search (pre-2010s):**
- Keyword-based search (TF-IDF, BM25)
- Good for exact matches, poor for understanding meaning
- Search engines returned documents - you had to read them yourself

**Early Question Answering Systems (2010s):**
- Watson (IBM, 2011) - Combined retrieval with rule-based reasoning
- Reading comprehension models - Could extract answers from provided passages
- Still relied heavily on keyword matching

**The Deep Learning Revolution (2017+):**
- Transformer models (2017) - "Attention is All You Need"
- BERT (2018) - Contextual understanding of language
- GPT-2/3 (2019/2020) - Large language models that could generate coherent text
- Dense vector representations - Text could be converted to meaningful vectors

**The Birth of Modern RAG (2020):**
The seminal paper "[Retrieval-Augmented Generation for Knowledge-Intensive NLP Tasks](https://arxiv.org/abs/2005.11401)" by Lewis et al. (Facebook AI, 2020) formally introduced RAG as we know it today. They combined:
- Dense retrieval (using learned vector representations)
- Pre-trained language models (BART)
- End-to-end training of the complete system

The results were impressive - RAG systems outperformed much larger models on knowledge-intensive tasks while being more efficient and up-to-date.

## Why RAG Took Off (2023-Present)

The explosion of LLMs like ChatGPT, GPT-4, and Claude made RAG essential:

1. **Hallucination Problem** - LLMs confidently make up facts. RAG grounds responses in real documents.
2. **Knowledge Cutoff** - LLMs trained on 2021 data don't know about 2024 events. RAG uses current documents.
3. **Private Data** - LLMs can't access your company's internal docs. RAG can.
4. **Cost** - Fine-tuning LLMs on custom data is expensive. RAG is cheaper and more flexible.
5. **Explainability** - RAG can cite sources, making it auditable and trustworthy.

As of 2024-2025, RAG is the de facto standard for building production AI systems that need accuracy and auditability.

# How RAG Works: The Complete Picture

Let's break down exactly what happens in a RAG system, from the moment you add a document to when a user gets an answer.

## Phase 1: Indexing (Preparing the Knowledge Base)

Before RAG can retrieve anything, you need to index your knowledge base. This is a one-time process (though you can add new documents later).

```mermaid
flowchart TB
    A[Source Documents] -->|1. Extract Text| B[Text Extraction]
    B -->|2. Split into Chunks| C[Chunking Service]
    C -->|3. Generate Embeddings| D[Embedding Model]
    D -->|4. Store Vectors| E[Vector Database]

    B -.Metadata.-> E

    subgraph "Example: Blog Post"
        F["# Title: Understanding Docker\n\nDocker is a containerization platform...\n\n## Benefits\n- Isolation\n- Portability"]
    end

    subgraph "Chunks"
        G["Chunk 1: Title + Intro"]
        H["Chunk 2: Benefits Section"]
    end

    subgraph "Embeddings"
        I["[0.234, -0.891, 0.567, ...]"]
        J["[0.445, -0.123, 0.789, ...]"]
    end

    F --> G
    F --> H
    G --> I
    H --> J

    style D fill:#f9f,stroke:#333,stroke-width:2px
    style E fill:#bbf,stroke:#333,stroke-width:2px
```

### Step 1: Text Extraction

Extract plain text from your source documents. This could be:
- Markdown files (like my blog posts)
- PDFs (for documentation)
- HTML (for web scraping)
- Database records
- Emails, chat logs, etc.

**Example from my blog:**
```csharp
// From MarkdownRenderingService
public string ExtractPlainText(string markdown)
{
    // Remove code blocks
    var withoutCode = Regex.Replace(markdown, @"```[\s\S]*?```", "");

    // Convert markdown to plain text
    var document = Markdown.Parse(withoutCode);
    var plainText = document.ToPlainText();

    return plainText.Trim();
}
```

### Step 2: Chunking

This is where most RAG implementations fail. You can't just split on paragraph boundaries - you need semantically coherent chunks.

**Why chunking matters:**
- LLMs have token limits (context windows)
- Smaller chunks = more precise retrieval
- But chunks must contain enough context to be meaningful

**Bad chunking:**
```
Chunk 1: "Docker is a containerization platform. It allows you"
Chunk 2: "to package applications with their dependencies. This"
Chunk 3: "ensures consistency across environments."
```

**Good chunking:**
```
Chunk 1: "Docker is a containerization platform. It allows you to package applications with their dependencies. This ensures consistency across environments."

Chunk 2: "Benefits of Docker:
- Isolation: Each container runs in its own environment
- Portability: Containers run anywhere Docker is installed
- Efficiency: Lightweight compared to virtual machines"
```

**Example from my semantic search implementation:**
```csharp
public class TextChunker
{
    private const int TargetChunkSize = 500; // ~500 words
    private const int ChunkOverlap = 50;     // 50 words overlap

    public List<Chunk> ChunkDocument(string text, string sourceId)
    {
        var chunks = new List<Chunk>();

        // Split on section boundaries first (## headers in markdown)
        var sections = SplitOnHeaders(text);

        foreach (var section in sections)
        {
            // If section is small enough, keep it whole
            if (section.WordCount < TargetChunkSize)
            {
                chunks.Add(new Chunk
                {
                    Text = section.Text,
                    SourceId = sourceId,
                    SectionHeader = section.Header
                });
            }
            else
            {
                // Split large sections on sentence boundaries
                var subChunks = SplitOnSentences(section.Text, TargetChunkSize, ChunkOverlap);
                chunks.AddRange(subChunks.Select(c => new Chunk
                {
                    Text = c,
                    SourceId = sourceId,
                    SectionHeader = section.Header
                }));
            }
        }

        return chunks;
    }
}
```

**Common chunking strategies:**
- **Fixed-size**: Simple but breaks semantic boundaries
- **Sentence-based**: Respects grammar but can be too small
- **Paragraph-based**: Natural but variable size
- **Section-based**: Best for structured content (my preference)
- **Sliding window with overlap**: Ensures no context is lost at boundaries

### Step 3: Generate Embeddings

Embeddings are the magic that makes semantic search possible. An embedding is a vector (array of numbers) that represents the meaning of text.

**Key concept:** Similar meanings → similar vectors

```
"Docker container" → [0.234, -0.891, 0.567, ..., 0.123]
"containerization platform" → [0.221, -0.903, 0.534, ..., 0.119]
"apple fruit" → [0.891, 0.234, -0.567, ..., -0.789]
```

The first two vectors would be "close" in vector space (high cosine similarity), while the third is far away.

**How embeddings are generated:**
Modern embedding models are neural networks trained on massive text datasets to learn semantic relationships. Popular models:
- **all-MiniLM-L6-v2**: 384 dimensions, fast, good quality (what I use on this blog)
- **text-embedding-3-small** (OpenAI): 1536 dimensions, very high quality
- **BGE-base**: 768 dimensions, state-of-the-art open source

**Example from my ONNX embedding service:**
```csharp
public async Task<float[]> GenerateEmbeddingAsync(string text)
{
    // Tokenize the input text
    var tokens = Tokenize(text);

    // Create input tensors for ONNX model
    var inputIds = CreateInputTensor(tokens);
    var attentionMask = CreateAttentionMaskTensor(tokens.Length);
    var tokenTypeIds = CreateTokenTypeIdsTensor(tokens.Length);

    // Run ONNX inference
    var inputs = new List<NamedOnnxValue>
    {
        NamedOnnxValue.CreateFromTensor("input_ids", inputIds),
        NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask),
        NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIds)
    };

    using var results = _session.Run(inputs);

    // Extract the output (sentence embedding)
    var output = results.First().AsTensor<float>();
    var embedding = output.ToArray();

    // L2 normalize the vector for cosine similarity
    return NormalizeVector(embedding);
}
```

**Why normalization matters:** After L2 normalization, cosine similarity becomes a simple dot product, making search much faster.

### Step 4: Store in Vector Database

Vector databases are optimized for storing and searching high-dimensional vectors. Unlike traditional databases that use SQL queries, vector databases use similarity search.

**Key operations:**
- **Upsert**: Add or update a vector with metadata
- **Search**: Find K most similar vectors to a query vector
- **Filter**: Combine vector search with metadata filters

**Example Qdrant implementation:**
```csharp
public async Task IndexDocumentAsync(
    string id,
    float[] embedding,
    Dictionary<string, object> metadata)
{
    var point = new PointStruct
    {
        Id = new PointId { Uuid = id },
        Vectors = embedding,
        Payload =
        {
            ["title"] = metadata["title"],
            ["source"] = metadata["source"],
            ["chunk_index"] = metadata["chunk_index"],
            ["created_at"] = DateTime.UtcNow.ToString("O")
        }
    };

    await _client.UpsertAsync(
        collectionName: "blog_posts",
        points: new[] { point }
    );
}
```

**Popular vector databases:**
- **Qdrant**: Fast, self-hostable, excellent C# support (my choice)
- **pgvector**: PostgreSQL extension (great if you're already using Postgres)
- **Pinecone**: Managed service (expensive but good)
- **Weaviate**: Feature-rich, good for complex schemas
- **ChromaDB**: Python-focused, lightweight

We'll explore setting up these databases in upcoming articles.

## Phase 2: Retrieval (Finding Relevant Information)

When a user asks a question, the RAG system needs to find the most relevant information from the knowledge base.

```mermaid
flowchart LR
    A[User Query:<br/>'How do I use Docker Compose?'] --> B[Generate Query Embedding]
    B --> C[Query Vector:<br/>[0.445, -0.123, 0.789, ...]]
    C --> D[Vector Search]
    D --> E[Vector Database]
    E --> F[Top K Similar Chunks]
    F --> G["1. Docker Compose Basics (score: 0.92)<br/>2. Multi-Container Setup (score: 0.87)<br/>3. Service Configuration (score: 0.83)"]

    style B fill:#f9f,stroke:#333,stroke-width:2px
    style D fill:#bbf,stroke:#333,stroke-width:2px
```

### Step 1: Generate Query Embedding

The user's question gets converted to a vector using the **same embedding model** used for indexing. This is critical - different models produce incompatible vectors.

```csharp
public async Task<List<SearchResult>> SearchAsync(string query, int limit = 10)
{
    // Same embedding model used for indexing
    var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);

    // Search in vector store
    var results = await _vectorStoreService.SearchAsync(
        queryEmbedding,
        limit
    );

    return results;
}
```

### Step 2: Similarity Search

The vector database computes similarity between the query vector and all stored vectors. Common metrics:

**Cosine Similarity** (most popular for normalized vectors):
```
similarity = (A · B) / (||A|| × ||B||)
```
Range: -1 to 1 (higher = more similar)

**Euclidean Distance** (for unnormalized vectors):
```
distance = sqrt(Σ(Ai - Bi)²)
```
Range: 0 to ∞ (lower = more similar)

**Dot Product** (when vectors are pre-normalized):
```
similarity = A · B
```
Range: -1 to 1 (higher = more similar)

**Example from my Qdrant service:**
```csharp
var searchResults = await _client.SearchAsync(
    collectionName: "blog_posts",
    vector: queryEmbedding,
    limit: (ulong)limit,
    scoreThreshold: 0.7f,  // Only return results with >70% similarity
    payloadSelector: true   // Include all metadata
);

return searchResults.Select(hit => new SearchResult
{
    Text = hit.Payload["text"].StringValue,
    Title = hit.Payload["title"].StringValue,
    Score = hit.Score,
    Source = hit.Payload["source"].StringValue
}).ToList();
```

### Step 3: Reranking (Optional but Recommended)

The initial retrieval is fast but approximate. Reranking uses a more sophisticated model to rescore the top K results.

```mermaid
flowchart LR
    A[Vector Search:<br/>Top 50 Results] --> B[Reranking Model]
    B --> C[Reranked:<br/>Top 10 Results]

    style B fill:#f9f,stroke:#333,stroke-width:2px
```

**Why reranking helps:**
- Fast embedding models optimize for speed, sacrificing some accuracy
- Reranking models are slower but more accurate
- Two-stage approach balances speed and quality

**Example reranking implementation:**
```csharp
public async Task<List<SearchResult>> SearchWithRerankAsync(
    string query,
    int initialLimit = 50,
    int finalLimit = 10)
{
    // Stage 1: Fast vector search
    var candidates = await SearchAsync(query, initialLimit);

    // Stage 2: Precise reranking
    var rerankedResults = await _rerankingService.RerankAsync(
        query,
        candidates
    );

    return rerankedResults.Take(finalLimit).ToList();
}
```

## Phase 3: Generation (Creating the Answer)

Now that we have relevant information, we feed it to the LLM along with the user's question.

```mermaid
flowchart TB
    A[User Query] --> B[Retrieved Context 1]
    A --> C[Retrieved Context 2]
    A --> D[Retrieved Context 3]

    B --> E[Construct Prompt]
    C --> E
    D --> E
    A --> E

    E --> F["System: You are a helpful assistant...\n\nContext:\n1. Docker Compose allows...\n2. Services are defined...\n3. Volumes persist data...\n\nQuestion: How do I use Docker Compose?\n\nAnswer:"]

    F --> G[LLM]
    G --> H[Generated Answer with Citations]

    style E fill:#f9f,stroke:#333,stroke-width:2px
    style G fill:#bbf,stroke:#333,stroke-width:2px
```

### Step 1: Prompt Construction

This is where RAG becomes an art. You need to structure the prompt so the LLM:
- Uses the provided context (not its internal knowledge)
- Cites sources when possible
- Admits when the context doesn't contain an answer
- Maintains a consistent tone/style

**Example prompt template from my Lawyer GPT system:**
```csharp
public string BuildRAGPrompt(string query, List<SearchResult> context)
{
    var sb = new StringBuilder();

    sb.AppendLine("You are a technical writing assistant. Your task is to answer the user's question using ONLY the provided context from past blog posts.");
    sb.AppendLine();
    sb.AppendLine("CONTEXT:");
    sb.AppendLine("========");

    for (int i = 0; i < context.Count; i++)
    {
        sb.AppendLine($"[{i + 1}] {context[i].Title}");
        sb.AppendLine($"Source: {context[i].Source}");
        sb.AppendLine($"Content: {context[i].Text}");
        sb.AppendLine($"Relevance: {context[i].Score:P0}");
        sb.AppendLine();
    }

    sb.AppendLine("========");
    sb.AppendLine();
    sb.AppendLine("INSTRUCTIONS:");
    sb.AppendLine("- Answer the question using the provided context");
    sb.AppendLine("- Cite sources using [1], [2], etc.");
    sb.AppendLine("- If the context doesn't contain enough information, say so");
    sb.AppendLine("- Maintain the technical, practical tone of the blog");
    sb.AppendLine();
    sb.AppendLine($"QUESTION: {query}");
    sb.AppendLine();
    sb.AppendLine("ANSWER:");

    return sb.ToString();
}
```

### Step 2: LLM Inference

The constructed prompt goes to the LLM for generation. This can be:
- **Cloud API**: OpenAI, Anthropic Claude, Google PaLM
- **Local model**: Using llama.cpp, ONNX Runtime, or TorchSharp

**Example using local LLM:**
```csharp
public async Task<string> GenerateResponseAsync(string prompt)
{
    var result = await _llamaSharp.InferAsync(prompt, new InferenceParams
    {
        Temperature = 0.7f,      // Creativity (0 = deterministic, 1 = creative)
        TopP = 0.9f,             // Nucleus sampling
        MaxTokens = 500,         // Response length limit
        StopSequences = new[] { "\n\n", "User:", "Question:" }
    });

    return result.Text.Trim();
}
```

**Key parameters explained:**
- **Temperature**: Controls randomness (0 = always pick most likely, 1 = sample randomly)
- **Top P**: Nucleus sampling - consider only tokens that make up top P probability mass
- **Max Tokens**: Limit response length
- **Stop Sequences**: When to stop generating

### Step 3: Post-Processing

After the LLM generates a response, we often need to:
- Extract citations and convert them to links
- Format code blocks
- Add metadata (sources, confidence scores)
- Log the interaction for debugging

**Example post-processing:**
```csharp
public RAGResponse PostProcess(string llmOutput, List<SearchResult> sources)
{
    var response = new RAGResponse
    {
        Answer = llmOutput,
        Sources = new List<Source>()
    };

    // Extract citations like [1], [2]
    var citations = Regex.Matches(llmOutput, @"\[(\d+)\]");

    foreach (Match match in citations)
    {
        int index = int.Parse(match.Groups[1].Value) - 1;
        if (index >= 0 && index < sources.Count)
        {
            var source = sources[index];
            response.Sources.Add(new Source
            {
                Title = source.Title,
                Url = GenerateUrl(source.Source),
                RelevanceScore = source.Score
            });
        }
    }

    // Convert markdown citations to hyperlinks
    response.FormattedAnswer = Regex.Replace(
        llmOutput,
        @"\[(\d+)\]",
        m => {
            int index = int.Parse(m.Groups[1].Value) - 1;
            if (index >= 0 && index < sources.Count)
            {
                var url = GenerateUrl(sources[index].Source);
                return $"[[{m.Groups[1].Value}]]({url})";
            }
            return m.Value;
        }
    );

    return response;
}
```

# Understanding LLM Internals: Tokens, KV Cache, and Context Windows

Before we compare RAG to other approaches, it's essential to understand how LLMs work internally. This knowledge helps you optimize RAG systems and avoid common pitfalls.

## What Are Tokens?

Tokens are the fundamental units that LLMs process. Text isn't fed directly to models - it's first broken down into tokens.

**Example tokenization:**
```
Input:  "Understanding Docker containers"
Tokens: ["Under", "standing", " Docker", " containers"]
```

Different models use different tokenization strategies:
- **GPT models**: Use Byte-Pair Encoding (BPE) with ~50K vocabulary
- **Claude**: Similar BPE approach
- **Llama models**: SentencePiece tokenization

**Why tokenization matters for RAG:**

```csharp
public class TokenCounter
{
    // Rough approximation: 1 token ≈ 0.75 words (English)
    public int EstimateTokens(string text)
    {
        var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        return (int)(wordCount / 0.75);
    }

    public int EstimateTokensAccurate(string text, ITokenizer tokenizer)
    {
        // Use actual tokenizer for precision
        return tokenizer.Encode(text).Count;
    }
}
```

**Context window limits:**
- GPT-3.5: 16K tokens
- GPT-4: 8K-128K tokens (depending on variant)
- Claude 3.5 Sonnet: 200K tokens
- Llama 3: 8K tokens (though can be extended)

In RAG systems, you must fit:
```
Total tokens = System prompt + Retrieved context + User query + Response buffer
```

If your RAG retrieves 10 documents of 500 tokens each, that's 5,000 tokens just for context - before the query and response!

**Practical RAG token management:**

```csharp
public class ContextWindowManager
{
    private readonly int _maxContextTokens;
    private readonly int _systemPromptTokens;
    private readonly int _responseBufferTokens;

    public ContextWindowManager(
        int totalContextWindow = 4096,
        int systemPromptTokens = 300,
        int responseBufferTokens = 500)
    {
        _maxContextTokens = totalContextWindow;
        _systemPromptTokens = systemPromptTokens;
        _responseBufferTokens = responseBufferTokens;
    }

    public List<SearchResult> FitContextInWindow(
        List<SearchResult> retrievedDocs,
        string query)
    {
        var queryTokens = EstimateTokens(query);

        // Available tokens for retrieved context
        var availableForContext = _maxContextTokens
            - _systemPromptTokens
            - queryTokens
            - _responseBufferTokens;

        var selectedDocs = new List<SearchResult>();
        var currentTokens = 0;

        foreach (var doc in retrievedDocs.OrderByDescending(d => d.Score))
        {
            var docTokens = EstimateTokens(doc.Text);

            if (currentTokens + docTokens <= availableForContext)
            {
                selectedDocs.Add(doc);
                currentTokens += docTokens;
            }
            else
            {
                break; // Context window full
            }
        }

        return selectedDocs;
    }

    private int EstimateTokens(string text)
    {
        // Rule of thumb: 1 token ≈ 4 characters
        return text.Length / 4;
    }
}
```

## The KV Cache: LLM's Secret Weapon

When an LLM generates text, it doesn't reprocess everything from scratch for each token. It uses a **Key-Value (KV) cache** to remember what it has already computed.

### How Transformers Work (Simplified)

Transformers use an "attention" mechanism where each token "attends to" (looks at) all previous tokens to understand context.

```mermaid
flowchart TB
    subgraph "Generation Step 1: 'Docker'"
        A1[Input: 'Docker'] --> B1[Compute K,V for 'Docker']
        B1 --> C1[Store in KV Cache]
        C1 --> D1[Generate: 'is']
    end

    subgraph "Generation Step 2: 'is'"
        A2[Input: 'is'] --> B2[Compute K,V for 'is']
        B2 --> C2[Store in KV Cache]
        C2 --> E2[Retrieve KV for 'Docker']
        E2 --> F2[Attend: 'is' to 'Docker']
        F2 --> D2[Generate: 'a']
    end

    subgraph "Generation Step 3: 'a'"
        A3[Input: 'a'] --> B3[Compute K,V for 'a']
        B3 --> C3[Store in KV Cache]
        C3 --> E3[Retrieve KV for 'Docker', 'is']
        E3 --> F3[Attend: 'a' to all previous]
        F3 --> D3[Generate: 'container']
    end

    D1 --> A2
    D2 --> A3

    style C1 fill:#f9f,stroke:#333,stroke-width:2px
    style C2 fill:#f9f,stroke:#333,stroke-width:2px
    style C3 fill:#f9f,stroke:#333,stroke-width:2px
```

**Without KV cache:**
- Step 1: Process 1 token → O(1)
- Step 2: Process 2 tokens from scratch → O(2)
- Step 3: Process 3 tokens from scratch → O(3)
- Total: O(1 + 2 + 3 + ... + N) = O(N²)

**With KV cache:**
- Step 1: Process 1 token, cache K,V → O(1)
- Step 2: Process 1 new token, reuse cached K,V → O(1)
- Step 3: Process 1 new token, reuse cached K,V → O(1)
- Total: O(N)

This makes generation **dramatically faster** - the difference between 10 tokens/second and 100 tokens/second.

### The KV Cache Tree Structure

The KV cache forms a "tree" because of how attention works in transformers. Each layer in the model has its own K,V matrices.

```mermaid
graph TB
    A[Input Tokens:<br/>'What is Docker?'] --> B[Layer 1 Attention]
    B --> C[Layer 1 KV Cache]

    B --> D[Layer 2 Attention]
    D --> E[Layer 2 KV Cache]

    D --> F[Layer 3 Attention]
    F --> G[Layer 3 KV Cache]

    F --> H[... up to Layer N]
    H --> I[Output: 'Docker is']

    C -.Key-Value pairs<br/>for all input tokens.-> C
    E -.Key-Value pairs<br/>for all input tokens.-> E
    G -.Key-Value pairs<br/>for all input tokens.-> G

    style C fill:#bbf,stroke:#333,stroke-width:2px
    style E fill:#bbf,stroke:#333,stroke-width:2px
    style G fill:#bbf,stroke:#333,stroke-width:2px
```

**Each layer stores:**
- **Keys (K)**: Representations used to compute attention scores
- **Values (V)**: Representations that get mixed together based on attention

For a model with:
- 32 layers
- 4096 hidden dimensions
- 32 attention heads
- 8K context window

The KV cache for one sequence is:
```
2 (K and V) × 32 layers × 4096 dimensions × 8192 tokens × 2 bytes (FP16)
≈ 4.3 GB of VRAM!
```

This is why long context windows are memory-intensive.

### KV Cache in RAG Systems

RAG systems can leverage KV cache optimization in clever ways:

**Prompt caching** (supported by some APIs like Anthropic Claude):

```csharp
public class CachedRAGService
{
    // System prompt and retrieved context can be cached!
    public async Task<string> GenerateWithCachedContextAsync(
        string systemPrompt,          // Cached
        List<SearchResult> context,   // Cached
        string userQuery)             // Not cached, changes each time
    {
        var contextText = FormatContext(context);

        // The KV cache for systemPrompt + contextText is reused across queries
        var prompt = $@"
{systemPrompt}

CONTEXT:
{contextText}

QUERY: {userQuery}

ANSWER:";

        return await _llm.GenerateAsync(prompt, useCaching: true);
    }
}
```

**Why this is powerful:**
- First query: Computes KV cache for system prompt + context (slow)
- Subsequent queries with same context: Reuses cached KV (10x faster!)
- Only the user query portion needs fresh computation

**Practical example:**
```
Query 1: "How do I use Docker?" → 2 seconds (no cache)
Query 2: "What are Docker benefits?" → 0.2 seconds (cache hit!)
Query 3: "Docker vs VMs?" → 0.2 seconds (cache hit!)
```

All three queries use the same retrieved context, so the KV cache for that context is reused.

## Token Limits and RAG Strategy

Understanding tokens and KV cache informs your RAG architecture decisions:

### 1. Chunking Size

Smaller chunks = more precise retrieval, but more overhead:

```csharp
// Option A: Small chunks (200 tokens each)
// Retrieve 20 chunks = 4,000 tokens
// Pro: Very precise, only relevant info
// Con: More KV cache entries, slower attention

// Option B: Larger chunks (500 tokens each)
// Retrieve 8 chunks = 4,000 tokens
// Pro: Better context coherence, fewer KV entries
// Con: More noise, less precise

public class AdaptiveChunker
{
    public int DetermineChunkSize(int contextWindowSize)
    {
        if (contextWindowSize <= 4096)
            return 200; // Small chunks for limited windows

        if (contextWindowSize <= 16384)
            return 500; // Medium chunks

        return 1000; // Large chunks for big windows
    }
}
```

### 2. Context Window Utilization

Don't max out the context window - leave room for generation:

```csharp
public class SafeContextManager
{
    public int GetSafeContextLimit(int totalContextWindow)
    {
        // Use only 75% for input, reserve 25% for output
        return (int)(totalContextWindow * 0.75);
    }

    // Example: 4K model
    // Total: 4096 tokens
    // Safe input: 3072 tokens
    // Reserved for output: 1024 tokens
}
```

### 3. Multi-Turn RAG Conversations

In chatbots, the conversation history grows with each turn:

```
Turn 1:
System + Context + Query1 = 3000 tokens
Response1 = 300 tokens
Total: 3300 tokens

Turn 2:
System + Context + Query1 + Response1 + Query2 = 3650 tokens
Response2 = 300 tokens
Total: 3950 tokens

Turn 3:
System + Context + Query1 + Response1 + Query2 + Response2 + Query3 = 4250 tokens
ERROR: Context window exceeded!
```

**Solution: Sliding window with re-retrieval**

```csharp
public class ConversationalRAG
{
    private readonly int _maxHistoryTokens = 1000;

    public async Task<string> ChatAsync(
        List<ConversationTurn> history,
        string newQuery)
    {
        // Re-retrieve context based on current query
        var context = await RetrieveContextAsync(newQuery);

        // Keep only recent conversation history
        var relevantHistory = TrimHistory(history, _maxHistoryTokens);

        var prompt = BuildPrompt(context, relevantHistory, newQuery);

        return await _llm.GenerateAsync(prompt);
    }

    private List<ConversationTurn> TrimHistory(
        List<ConversationTurn> history,
        int maxTokens)
    {
        var trimmed = new List<ConversationTurn>();
        var currentTokens = 0;

        // Keep most recent turns
        foreach (var turn in history.Reverse())
        {
            var turnTokens = EstimateTokens(turn.Query) + EstimateTokens(turn.Response);

            if (currentTokens + turnTokens <= maxTokens)
            {
                trimmed.Insert(0, turn);
                currentTokens += turnTokens;
            }
            else
            {
                break;
            }
        }

        return trimmed;
    }
}
```

### 4. Token Cost Optimization

API-based LLMs charge per token. RAG can explode costs if not careful:

```csharp
public class CostAwareRAG
{
    // OpenAI GPT-4 pricing (example):
    // Input: $0.03 per 1K tokens
    // Output: $0.06 per 1K tokens

    public decimal EstimateQueryCost(
        int systemPromptTokens,
        int retrievedContextTokens,
        int queryTokens,
        int expectedResponseTokens)
    {
        var inputTokens = systemPromptTokens + retrievedContextTokens + queryTokens;
        var outputTokens = expectedResponseTokens;

        var inputCost = (inputTokens / 1000m) * 0.03m;
        var outputCost = (outputTokens / 1000m) * 0.06m;

        return inputCost + outputCost;
    }

    // Example:
    // System: 300 tokens
    // Context: 3000 tokens (10 retrieved docs)
    // Query: 50 tokens
    // Response: 500 tokens
    //
    // Cost = ((300 + 3000 + 50) / 1000 * 0.03) + (500 / 1000 * 0.06)
    //      = (3350 / 1000 * 0.03) + (500 / 1000 * 0.06)
    //      = $0.1005 + $0.03
    //      = $0.1305 per query
    //
    // At 1000 queries/day = $130/day = $3,900/month!
}
```

**Cost reduction strategies:**
1. Retrieve fewer, better-ranked documents
2. Use prompt caching (Anthropic Claude: 90% cheaper for cached tokens)
3. Use cheaper models for re-ranking, expensive for final generation
4. Compress context using summarization

## Visualizing the Complete RAG Flow with Tokens

Here's how tokens, KV cache, and RAG fit together:

```mermaid
flowchart TB
    A[User Query:<br/>'How does Docker work?'<br/>≈ 12 tokens] --> B[Generate Query Embedding]

    B --> C[Vector Search]
    C --> D[Retrieved Docs:<br/>5 docs × 500 tokens<br/>= 2,500 tokens]

    D --> E[Construct Prompt]
    A --> E

    E --> F["Complete Prompt:<br/>System: 300 tokens<br/>Context: 2,500 tokens<br/>Query: 12 tokens<br/>Total: 2,812 tokens"]

    F --> G[Tokenize Prompt]
    G --> H["Token IDs:<br/>[245, 1034, 8829, ...]<br/>2,812 token IDs"]

    H --> I[LLM Layer 1]
    I --> J[Compute K,V]
    J --> K[KV Cache Layer 1:<br/>2,812 K,V pairs]

    I --> L[LLM Layer 2]
    L --> M[Compute K,V]
    M --> N[KV Cache Layer 2:<br/>2,812 K,V pairs]

    L --> O[... Layers 3-32]
    O --> P[Generate Token 1: 'Docker']

    P --> Q[Add to KV Cache]
    Q --> R[Generate Token 2: 'is']
    R --> S[Add to KV Cache]
    S --> T[... until completion]

    T --> U["Response: 'Docker is a containerization platform...'<br/>≈ 400 tokens"]

    style K fill:#f9f,stroke:#333,stroke-width:2px
    style N fill:#f9f,stroke:#333,stroke-width:2px
    style Q fill:#bbf,stroke:#333,stroke-width:2px
    style S fill:#bbf,stroke:#333,stroke-width:2px
```

**Key insights:**
1. **Input tokens** (2,812) are processed once to build initial KV cache
2. **Generation** happens one token at a time, reusing the KV cache
3. **Each new token** adds to the KV cache for future tokens to attend to
4. **Total VRAM** needed = Model weights + KV cache for all tokens
5. **Longer context** = larger KV cache = more VRAM

## Practical Implications for RAG

Understanding tokens and KV cache leads to better RAG design:

**1. Pre-compute and cache common contexts:**
```csharp
// Cache KV for frequently used system prompts + static context
var cachedSystemContext = await _llm.PrecomputeKVCache(systemPrompt + staticContext);

// Reuse for each query (much faster)
foreach (var query in userQueries)
{
    var response = await _llm.GenerateAsync(query, reuseKVCache: cachedSystemContext);
}
```

**2. Optimize chunk boundaries:**
```csharp
// Bad: Arbitrary 500-character chunks
var chunks = text.Chunk(500);

// Good: Chunk on sentence boundaries, measure in tokens
public List<string> ChunkByTokens(string text, int maxTokensPerChunk)
{
    var sentences = SplitIntoSentences(text);
    var chunks = new List<string>();
    var currentChunk = new StringBuilder();
    var currentTokens = 0;

    foreach (var sentence in sentences)
    {
        var sentenceTokens = EstimateTokens(sentence);

        if (currentTokens + sentenceTokens > maxTokensPerChunk && currentTokens > 0)
        {
            chunks.Add(currentChunk.ToString());
            currentChunk.Clear();
            currentTokens = 0;
        }

        currentChunk.Append(sentence).Append(" ");
        currentTokens += sentenceTokens;
    }

    if (currentTokens > 0)
        chunks.Add(currentChunk.ToString());

    return chunks;
}
```

**3. Monitor token usage in production:**
```csharp
public class RAGTelemetry
{
    public void LogRAGQuery(
        string query,
        List<SearchResult> retrievedDocs,
        string response)
    {
        var queryTokens = EstimateTokens(query);
        var contextTokens = retrievedDocs.Sum(d => EstimateTokens(d.Text));
        var responseTokens = EstimateTokens(response);
        var totalTokens = queryTokens + contextTokens + responseTokens;

        _logger.LogInformation(
            "RAG Query: {Query} | Context: {ContextTokens} tokens from {DocCount} docs | " +
            "Response: {ResponseTokens} tokens | Total: {TotalTokens} tokens",
            query, contextTokens, retrievedDocs.Count, responseTokens, totalTokens
        );

        // Alert if approaching context limit
        if (totalTokens > _maxTokens * 0.9)
        {
            _logger.LogWarning("Approaching token limit: {TotalTokens}/{MaxTokens}",
                totalTokens, _maxTokens);
        }
    }
}
```

# RAG vs. Other Approaches

Let's compare RAG to other methods of augmenting LLMs with knowledge.

## RAG vs. Fine-Tuning

| Aspect | RAG | Fine-Tuning |
|--------|-----|-------------|
| **Knowledge Updates** | Instant (just update the knowledge base) | Requires retraining |
| **Cost** | Low (storage + embedding) | High (GPU training time) |
| **Accuracy** | Grounded in sources | Can hallucinate |
| **Customization** | Limited to retrieval | Deep model adaptation |
| **Explainability** | High (can cite sources) | Low (black box) |
| **Best For** | Knowledge-intensive tasks | Style/format adaptation |

**When to use Fine-Tuning:**
- You need the model to learn a specific **style** or **format**
- You have a large, clean dataset
- You need the model to internalize **patterns**, not facts
- Example: Making GPT write like Shakespeare

**When to use RAG:**
- You need **factual accuracy** with citations
- Your knowledge base changes frequently
- You have private/proprietary data
- Cost and simplicity matter
- Example: Customer support chatbot (my company's docs change weekly)

**Can you combine both?** Yes! Fine-tune for style, RAG for facts.

## RAG vs. Long Context Windows

Modern LLMs boast huge context windows (GPT-4: 128K tokens, Claude: 200K tokens). Why not just dump all your documents into the context?

**Problems with long context:**
1. **Cost**: Pricing scales with tokens - $10-100 per query adds up fast
2. **Latency**: Processing 100K tokens takes time
3. **Lost in the middle**: LLMs struggle to use information in the middle of long contexts
4. **Dilution**: Relevant info gets buried in noise
5. **Practical limits**: You can't fit your entire company's docs in context

**When long context makes sense:**
- Single large document (e.g., analyzing a contract)
- Everything is relevant (no need to filter)
- Cost isn't a concern
- Low query volume

**When RAG makes sense:**
- Large knowledge base (millions of documents)
- Need to find relevant subset
- High query volume (cost matters)
- Real-time updates to knowledge

**Best practice:** Use RAG to select the most relevant content, then use long context for that subset.

## RAG vs. Prompting with Examples

Few-shot prompting (giving examples in the prompt) is a simple baseline.

**Example prompt:**
```
Examples:
Q: What is Docker?
A: Docker is a containerization platform...

Q: How does Kubernetes work?
A: Kubernetes orchestrates containers...

Q: What is my new question?
A: [LLM generates answer]
```

**Limitations:**
- Limited to what fits in context window
- Manual curation of examples
- Doesn't scale to large knowledge bases
- No semantic search - you manually pick examples

**RAG improvement:**
- Automatically finds best examples (via similarity search)
- Scales to unlimited examples (only top K sent to LLM)
- Adapts to query (different queries retrieve different examples)

You can think of RAG as "automated few-shot prompting at scale."

## Hybrid Search: RAG + Traditional Search

You can combine RAG with traditional full-text search using Reciprocal Rank Fusion (RRF).

**Why hybrid?**
- **Semantic search**: Great for conceptual matches ("error handling" finds "exception management")
- **Keyword search**: Great for exact terms ("Docker Compose", "Entity Framework")
- **Hybrid**: Best of both worlds

```csharp
public async Task<List<SearchResult>> HybridSearchAsync(string query)
{
    // Run both searches in parallel
    var semanticTask = SemanticSearchAsync(query, limit: 20);
    var keywordTask = KeywordSearchAsync(query, limit: 20);

    await Task.WhenAll(semanticTask, keywordTask);

    var semanticResults = await semanticTask;
    var keywordResults = await keywordTask;

    // Combine using Reciprocal Rank Fusion
    return ApplyRRF(semanticResults, keywordResults);
}

private List<SearchResult> ApplyRRF(
    List<SearchResult> list1,
    List<SearchResult> list2,
    int k = 60)
{
    var scores = new Dictionary<string, double>();

    // Score from first list
    for (int i = 0; i < list1.Count; i++)
    {
        var id = list1[i].Id;
        scores[id] = scores.GetValueOrDefault(id, 0) + 1.0 / (k + i + 1);
    }

    // Score from second list
    for (int i = 0; i < list2.Count; i++)
    {
        var id = list2[i].Id;
        scores[id] = scores.GetValueOrDefault(id, 0) + 1.0 / (k + i + 1);
    }

    // Merge and sort by combined score
    var allResults = list1.Concat(list2)
        .GroupBy(r => r.Id)
        .Select(g => g.First())
        .OrderByDescending(r => scores[r.Id])
        .ToList();

    return allResults;
}
```

# Summary and Next Steps

In this first part of the RAG primer, we've covered the theoretical foundations:

**What we learned:**
- RAG combines retrieval (semantic search) with generation (LLMs) for accurate, grounded responses
- The three phases: Indexing (chunking, embedding, storage), Retrieval (search, reranking), Generation (prompting, inference)
- How LLMs work internally: tokens, KV cache, context windows
- RAG compared to alternatives: fine-tuning, long context, few-shot prompting

**Key takeaways:**
- RAG separates knowledge storage from reasoning
- Proper chunking and embedding are critical for retrieval quality
- Understanding token limits and KV cache helps optimize RAG systems
- RAG excels at knowledge-intensive tasks that need accuracy and citations

**Continue to [Part 2: Practical Applications](/blog/rag-primer-practical)** where we'll dive into:
- Real-world RAG implementations on this blog
- Common challenges and how to solve them
- Advanced techniques (HyDE, multi-query, contextual compression, long-term memory)
- Step-by-step guide to building your own RAG system

# Resources

**Foundational Papers:**
- [Retrieval-Augmented Generation for Knowledge-Intensive NLP Tasks](https://arxiv.org/abs/2005.11401) - The original RAG paper
- [Dense Passage Retrieval for Open-Domain Question Answering](https://arxiv.org/abs/2004.04906) - DPR (retrieval foundation)
- [Attention Is All You Need](https://arxiv.org/abs/1706.03762) - Transformers (embedding foundation)

**Tools and Frameworks:**
- [Qdrant](https://qdrant.tech/) - Vector database I use
- [ONNX Runtime](https://onnxruntime.ai/) - For local embeddings
- [LLamaSharp](https://github.com/SciSharp/LLamaSharp) - For local LLM inference
- [Sentence Transformers](https://www.sbert.net/) - Embedding models

**Further Reading:**
- [Anthropic: Contextual Retrieval](https://www.anthropic.com/index/contextual-retrieval) - Advanced RAG techniques
- [How Neural Machine Translation Works](/blog/how-neural-machine-translation-works) - Understanding the AI models behind embeddings

Happy building, and see you in Part 2!
