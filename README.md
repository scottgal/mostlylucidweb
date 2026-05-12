# mostlylucid

[![Live Site](https://img.shields.io/badge/Live%20Site-mostlylucid.net-blue)](https://mostlylucid.net)
[![License: Unlicense](https://img.shields.io/badge/License-Unlicense-green.svg)](https://unlicense.org/)

This repository contains the source code for [mostlylucid.net](https://mostlylucid.net) - the personal site and blog of Scott Galloway, a consulting web developer and systems architect with over 30 years of experience building web applications.

**🌐 Visit the live site:** [mostlylucid.net](https://mostlylucid.net)

---

## CLI Tools

Self-contained, portable executables - no runtime install required.

### DataSummarizer

[![.NET](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/)

Fast, local, privacy-first data profiling with DuckDB + optional LLM narration. Profile any CSV, Excel, Parquet, JSON, SQLite, or log file in seconds.

```bash
# Quick profile (pure stats, no LLM)
datasummarizer -f data.csv --no-llm --fast

# Q&A mode with LLM insights
datasummarizer -f data.csv --query "what drives churn?" --model qwen2.5-coder:7b

# Profile Apache/IIS log files
datasummarizer -f /var/log/apache2/error.log --no-llm --fast
```

**Features:**
- 52K+ rows profiled in ~1 second
- Privacy-safe PII detection (hidden by default)
- Drift detection and constraint validation
- Natural language Q&A with SQL generation
- Log file support (Apache error/access, IIS W3C)
- ONNX embeddings for semantic caching

📖 [Full Documentation](./Mostlylucid.DataSummarizer/README.md)

---

### DocSummarizer

[![.NET](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/)

Turn documents or URLs into evidence-grounded summaries - every claim cites its source. Runs entirely on your machine.

```bash
# Fast extractive summary (no LLM, ~3s)
docsummarizer -f document.pdf -m Bert

# With LLM synthesis (~10s)
docsummarizer -f report.pdf -m BertRag

# Summarize a URL
docsummarizer -u https://example.com/article
```

**Features:**
- PDF, DOCX, PPTX, XLSX, Markdown, HTML support
- Bert mode: pure ONNX, no LLM, ~1-3 seconds
- BertRag mode: LLM synthesis with citations
- Multiple output formats (executive, bullets, bookreport)
- MCP server for AI agent integration

📖 [Full Documentation](./Mostlylucid.DocSummarizer/README.md)

---

## NPM Packages

[![npm version](https://img.shields.io/npm/v/@mostlylucid/mermaid-enhancements.svg)](https://www.npmjs.com/package/@mostlylucid/mermaid-enhancements)
[![npm downloads](https://img.shields.io/npm/dm/@mostlylucid/mermaid-enhancements.svg)](https://www.npmjs.com/package/@mostlylucid/mermaid-enhancements)

### @mostlylucid/mermaid-enhancements

Enhances Mermaid diagrams with export, panning, zoom, expanding lightbox, and theme switching.

```bash
npm install @mostlylucid/mermaid-enhancements
```

📖 [npm package](https://www.npmjs.com/package/@mostlylucid/mermaid-enhancements)

---

## NuGet Packages

### Mostlylucid.MinimalBlog

[![NuGet](https://img.shields.io/nuget/v/Mostlylucid.MinimalBlog.svg)](https://www.nuget.org/packages/Mostlylucid.MinimalBlog)

A minimal, file-based markdown blog for ASP.NET Core. Just point to a folder of markdown files and go.

```csharp
builder.Services.AddMinimalBlog(options =>
{
    options.MarkdownPath = "Markdown";
});
app.UseMinimalBlog();
```

**Features:**
- ~500 lines of code total
- Memory and output caching built-in
- MetaWeblog API support for external editors
- GitHub-inspired dark theme

📖 [Full Documentation](./Mostlylucid.MinimalBlog/README.md)

---

### Mostlylucid.Markdig.FetchExtension

[![NuGet](https://img.shields.io/nuget/v/mostlylucid.Markdig.FetchExtension.svg)](https://www.nuget.org/packages/mostlylucid.Markdig.FetchExtension)
[![NuGet](https://img.shields.io/nuget/dt/mostlylucid.Markdig.FetchExtension.svg)](https://www.nuget.org/packages/mostlylucid.Markdig.FetchExtension)

A complete solution for fetching and caching remote markdown content with multiple storage backends, automatic polling, and stale-while-revalidate caching.

📖 [NuGet Package](https://www.nuget.org/packages/mostlylucid.Markdig.FetchExtension/)

---

### Mostlylucid.MockLlmApi

[![NuGet](https://img.shields.io/nuget/v/mostlylucid.mockllmapi.svg)](https://www.nuget.org/packages/mostlylucid.mockllmapi)
[![NuGet](https://img.shields.io/nuget/dt/mostlylucid.mockllmapi.svg)](https://www.nuget.org/packages/mostlylucid.mockllmapi)

Lightweight ASP.NET Core middleware for generating realistic mock API responses using local LLMs (via Ollama). Add intelligent mock endpoints with 2 lines of code.

📖 [NuGet Package](https://www.nuget.org/packages/mostlylucid.mockllmapi)

---

### Mostlylucid.PagingTagHelper

[![NuGet](https://img.shields.io/nuget/v/mostlylucid.pagingtaghelper.svg)](https://www.nuget.org/packages/mostlylucid.pagingtaghelper)
[![NuGet](https://img.shields.io/nuget/dt/mostlylucid.pagingtaghelper.svg)](https://www.nuget.org/packages/mostlylucid.pagingtaghelper)

HTMX-enabled ASP.NET Core Tag Helpers for paging tasks.

📖 [NuGet Package](https://www.nuget.org/packages/mostlylucid.pagingtaghelper)

---

### Umami.Net

[![NuGet](https://img.shields.io/nuget/v/Umami.Net.svg)](https://www.nuget.org/packages/Umami.Net)
[![NuGet](https://img.shields.io/nuget/dt/Umami.Net.svg)](https://www.nuget.org/packages/Umami.Net)

A .NET client for Umami Web Analytics - privacy-focused analytics integration.

📖 [NuGet Package](https://www.nuget.org/packages/Umami.Net)

---

### MostlyLucid.EufySecurity

[![NuGet](https://img.shields.io/nuget/v/MostlyLucid.EufySecurity.svg)](https://www.nuget.org/packages/MostlyLucid.EufySecurity)
[![NuGet](https://img.shields.io/nuget/dt/MostlyLucid.EufySecurity.svg)](https://www.nuget.org/packages/MostlyLucid.EufySecurity)

**HIGHLY EXPERIMENTAL** .NET client library for controlling Eufy Security devices via cloud and P2P connections.

📖 [NuGet Package](https://www.nuget.org/packages/MostlyLucid.EufySecurity)

---

## Additional Projects

| Project | Description |
|---------|-------------|
| **[TinyLLM](./TinyLLM/)** | Windows WPF app for local AI chat with RAG memory. Supports Ollama and direct GGUF loading. |
| **[Chat.Server](./Mostlylucid.Chat.Server/)** | SignalR hub for real-time chat between website visitors and administrators. |
| **[Chat.Widget](./Mostlylucid.Chat.Widget/)** | Embeddable JavaScript chat widget for any website. |
| **[SemanticSearch](./Mostlylucid.SemanticSearch/)** | CPU-friendly semantic search using ONNX embeddings and Qdrant. |
| **[BlogLLM](./mostlylucid.blogllm/)** | RAG knowledge base builder for markdown documents. |
| **[SemanticGallery.Demo](./Mostlylucid.SemanticGallery.Demo/)** | Image gallery with semantic search capabilities. |
| **[SentimentAnalysis](./Mostlylucid.SentimentAnalysis/)** | Local sentiment analysis using ONNX models. |
| **[Workflow.Engine](./Mostlylucid.Workflow.Engine/)** | Simple workflow execution engine. |

---

## Live DataSummarizer Session Example

> Clarifier sentinel (tiny LLM) is enabled by default. Configure via `DataSummarizerSettings`:
> - `EnableClarifierSentinel` (default: true)
> - `ClarifierSentinelModel` (default: `qwen2.5:1.5b`; falls back to heuristics if unavailable)

```
File: Rotten Tomatoes Movies.csv
Session: 21380e65cf9d4db18305d7950aee1b2f

> What is the average?
Average over which dimension? Pick one:
- overall (no grouping)
- by rating
- by tomatometer_status
- by runtime_in_minutes quartiles
SQL: /* clarification required: average dimension not specified (no SQL executed) */

> overall
The average runtime of the movies is 102.39 minutes, with an average
Tomatometer score of 60.47 and an average audience rating of 60.47.
SQL:
SELECT AVG("runtime_in_minutes") AS avg_runtime,
       AVG("tomatometer_rating") AS avg_tomatometer,
       AVG("audience_rating") AS avg_audience
FROM read_csv_auto('sampledata/Rotten+Tomatoes+Movies.csv/Rotten Tomatoes Movies.csv');
```

---

## Featured Articles

Dive into some of the technical deep-dives and experiments from the blog:

### 📦 Package Development
- **[Building a Remote Markdown Fetcher for Markdig](https://www.mostlylucid.net/blog/markdigfetchextension)** - A complete guide to building a Markdig extension with caching, remote content fetching, and TOC generation
- **[Creating an LLM Mock API with Ollama](https://www.mostlylucid.net/blog/llmapi)** - Add intelligent mock endpoints to any project with just 2 lines of code
- **[Umami.Net: Analytics Made Simple](https://www.mostlylucid.net/blog/category/Umami)** - Building a .NET client for privacy-focused web analytics

### 🎨 Frontend & HTMX
- **[HTMX & Alpine.js Integration Patterns](https://www.mostlylucid.net/blog/htmxandaspnetcore)** - Building dynamic UIs without heavy JavaScript frameworks
- **[Mermaid Diagrams with Interactive Controls](https://www.mostlylucid.net/blog/enhancingmermaiddiagramswithpanzoomandexport)** - Pan, zoom, and fullscreen diagram support

### 🚀 DevOps & Infrastructure
- **[Docker Multi-Stage Builds for .NET](https://www.mostlylucid.net/blog/dockercomposedevdeps)** - Optimizing container images and deployment workflows
- **[Monitoring with Prometheus & Grafana](https://www.mostlylucid.net/blog/usingprometheusandgrafanatomonitoraspnet)** - Setting up observability for ASP.NET Core applications

### 🤖 AI & Translation
- **[Automated Blog Translation with EasyNMT](https://www.mostlylucid.net/blog/autotranslatingmarkdownfiles)** - Building a multilingual blog with automated translation to 12 languages

[**→ View all articles**](https://mostlylucid.net/blog)

---

## About the Site

**mostlylucid** is a living lab: part portfolio, part blog, part playground. It's where I share:

- **Technical deep dives** into ASP.NET Core, JavaScript frameworks (HTMX, Alpine.js), Docker, cloud, and search technologies
- **Experiments** with modern frontend stacks (Tailwind, DaisyUI, Markdown tooling, Mermaid diagrams)
- **Open-source contributions** like NuGet packages, tag helpers, and utilities
- **Reflections** on freelancing, remote work, and the craft of building resilient systems

The site is intentionally a work in progress - things may break, evolve, or get rebuilt entirely. That's part of the ethos: showing how things are built, not just the polished result.

### Platform Features

- **Dual-mode blog system**: File-based markdown or PostgreSQL database storage
- **Multilingual support**: Automated translation to 12 languages via EasyNMT
- **Full-text search**: PostgreSQL-powered search with tsvector/GIN indexes
- **Comments system**: Nested comments with closure table pattern
- **Analytics**: Integrated Umami analytics with custom .NET client
- **Observability**: Comprehensive logging (Serilog + Seq), metrics (Prometheus), and tracing

## Tech Stack

### Backend
- **.NET 10** - ASP.NET Core MVC for server-side rendering
- **PostgreSQL 16** - Primary database with full-text search
- **Entity Framework Core** - ORM with code-first migrations
- **Hangfire** - Background job processing for newsletters
- **DuckDB** - In-process analytics for data profiling

### Frontend
- **HTMX 2.0** - Server-driven interactions without heavy JavaScript
- **Alpine.js** - Lightweight reactive components
- **Tailwind CSS + DaisyUI** - Utility-first styling with component library
- **Highlight.js** - Syntax highlighting with custom Razor support
- **Mermaid** - Interactive diagram rendering
- **EasyMDE** - Markdown editor

### AI/ML
- **Ollama** - Local LLM inference
- **ONNX Runtime** - CPU-optimized embeddings and inference
- **Qdrant** - Vector database for semantic search
- **DuckDB VSS** - Vector similarity search extension

### Infrastructure
- **Docker** - Containerized deployment with Docker Compose
- **Caddy** - Reverse proxy and automatic HTTPS
- **Prometheus + Grafana** - Metrics and visualization
- **Seq** - Structured log aggregation
- **Umami** - Privacy-focused web analytics
- **Cloudflare Tunnel** - Secure remote access

## Getting Started

### Prerequisites
- .NET 10 SDK (for building from source)
- Node.js (for frontend builds)
- Docker (optional, for full stack)

### Quick Start

```bash
# Clone the repository
git clone https://github.com/scottgal/mostlylucidweb.git
cd mostlylucidweb

# Install dependencies
npm install
dotnet restore

# Build frontend assets
npm run build

# Run the development server
dotnet run --project Mostlylucid/Mostlylucid.csproj
```

The site will be available at `https://localhost:5001`

### Docker Development

For the full stack including database, analytics, and monitoring:

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f mostlylucid
```

See [CLAUDE.md](./CLAUDE.md) for detailed development documentation.

## About Scott

**Scott Galloway** is a consulting web developer and systems architect with over 30 years of experience building web applications. As a former **Microsoft ASP.NET Program Manager**, he has deep expertise in the .NET ecosystem and has worked with everyone from Fortune 500 companies to early-stage startups.

### What I Do

- **Full-stack development** across .NET and JavaScript ecosystems
- **Systems architecture** and rapid prototyping for complex web applications
- **Open-source development** - Building and maintaining NuGet packages, libraries, and tools
- **Remote team leadership** and startup bootstrapping
- **Technical writing** - Sharing knowledge through detailed technical articles

### Current Focus

- Building developer tools and libraries for the .NET community
- Exploring modern web patterns with HTMX and Alpine.js
- Experimenting with AI/LLM integration in web applications
- Creating privacy-focused analytics and monitoring solutions
- Contributing to the Markdig ecosystem with custom extensions

### Background

With a career spanning from the early days of ASP.NET to modern cloud-native architectures, I've been fortunate to work on:

- Large-scale enterprise applications serving millions of users
- Developer tools and frameworks used by thousands of developers
- Startup MVPs that needed to scale quickly and reliably
- Open-source projects that solve real-world problems

I believe in **building in public** - sharing both successes and failures, showing the messy middle of development, not just the polished result.

### Let's Connect

Interested in collaborating, consulting, or just chatting about building things?

- **Email:** scott.galloway+ml@gmail.com
- **Blog:** [mostlylucid.net](https://mostlylucid.net)
- **GitHub:** [@scottgal](https://github.com/scottgal)

## Contributing

This repo is primarily Scott's personal playground, but feedback, issues, and suggestions are welcome. Feel free to open a PR or drop a note.

## License

**Unlicense** - This is free and unencumbered software released into the public domain. See [LICENSE](./LICENSE) for details.