# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Mostlylucid is a sophisticated .NET 9.0 blog platform with multilingual support, automated translation, full-text search, and comprehensive observability. The blog supports dual content modes (file-based and database-driven) and integrates with external services for analytics, translation, and monitoring.

## Build & Development Commands

### .NET Backend

```bash
# Build the entire solution
dotnet build Mostlylucid.sln

# Build specific project
dotnet build Mostlylucid/Mostlylucid.csproj

# Run the main web application (development)
dotnet run --project Mostlylucid/Mostlylucid.csproj

# Run the scheduler service
dotnet run --project Mostlylucid.SchedulerService/Mostlylucid.SchedulerService.csproj

# Run all tests
dotnet test

# Run tests for specific project
dotnet test Mostlylucid.Test/Mostlylucid.Test.csproj
dotnet test Mostlylucid.Service.Test/Mostlylucid.Service.Test.csproj
dotnet test Umami.Net.Test/Umami.Net.Test.csproj

# Create a new migration (from solution root)
dotnet ef migrations add MigrationName --project Mostlylucid.DbContext --startup-project Mostlylucid

# Apply migrations
dotnet ef database update --project Mostlylucid.DbContext --startup-project Mostlylucid
```

### Frontend Assets

```bash
# Navigate to Mostlylucid directory first
cd Mostlylucid

# Install dependencies
npm install

# Development build (one-time)
npm run dev

# Watch mode for development
npm run watch

# Production build
npm run build

# Individual build steps (rarely needed)
npm run dev:tw-once       # Build TailwindCSS
npm run dev:copy-once     # Copy static CSS files
npm run dev:js            # Build JavaScript
```

### Docker

```bash
# Start all services (main app, database, Umami, translation service, monitoring)
docker-compose up -d

# Start only development dependencies (database, etc.)
docker-compose -f devdeps-docker-compose.yml up -d

# View logs
docker-compose logs -f mostlylucid

# Rebuild and restart
docker-compose up -d --build mostlylucid
```

### Podman (Docker Alternative)

```bash
# Start all services with Podman
podman-compose -f podman-compose.yml up -d

# Start only development dependencies
podman-compose -f devdeps-podman-compose.yml up -d

# View logs
podman-compose -f podman-compose.yml logs -f mostlylucid

# Build images with Podman (Dockerfiles work unchanged)
podman build -t scottgal/mostlylucid:latest -f Mostlylucid/Dockerfile .

# Setup systemd services (for production)
podman generate systemd --new --files --name mostlylucid

# Enable auto-updates
sudo systemctl enable --now podman-auto-update.timer

# See PODMAN.md for complete migration guide
```

## Solution Architecture

### Project Structure

- **Mostlylucid** - Main ASP.NET Core MVC web application
- **Mostlylucid.DbContext** - Entity Framework Core database layer (PostgreSQL)
- **Mostlylucid.Services** - Business logic layer
- **Mostlylucid.Shared** - Shared models, entities, DTOs, configuration classes
- **Mostlylucid.SchedulerService** - Background job scheduler (Hangfire) for newsletters
- **Umami.Net** - Custom analytics client library for Umami
- **Test Projects** - Unit and integration tests (xUnit, Moq)

### Dual-Mode Blog System

The blog supports two operational modes configured via `appsettings.json` → `Blog.Mode`:

1. **File Mode**: Markdown files stored on disk (`Mostlylucid/Markdown/`)
   - Served via `MarkdownBlogViewService`
   - Fast development iteration
   - File watcher (`MarkdownDirectoryWatcherService`) monitors changes

2. **Database Mode** (Production): Content stored in PostgreSQL
   - Served via `BlogPostViewService`
   - Full-text search using PostgreSQL `tsvector` with GIN indexes
   - The file watcher still runs, syncing file changes to the database
   - Entities: `BlogPostEntity`, `CategoryEntity`, `LanguageEntity`, `CommentEntity`

### Key Services & Patterns

**Markdown Processing** (`MarkdownRenderingService`):
- Extracts metadata from markdown files:
  - Title from first line
  - Categories from HTML comments: `<!-- category -- Cat1, Cat2 -->`
  - Published date from hidden datetime elements
- Converts to HTML and plain text using Markdig library
- Generates table of contents via Leisn.MarkdigToc

**Translation Service** (`MarkdownTranslatorService`):
- Automatically translates blog posts to 12 languages
- Uses EasyNMT translation API (Docker container)
- Batches text extraction from Markdown AST to avoid translating code blocks, URLs
- Round-robin load balancing across multiple translation service IPs
- Hash-based change detection (`.hash` files in `Markdown/translated/`)
- Translated files: `{slug}.{language}.md`

**Comment System**:
- Nested comments using closure table pattern (`CommentClosure` entity)
- Markdown storage in `Markdown/comments/` and `Markdown/notmoderatedcomments/`
- Controller: `CommentController`

**Newsletter System**:
- Separate scheduler service using Hangfire
- FluentEmail with Razor templates for email generation
- Subscription tracking via `EmailSubscriptionEntity` and `EmailSubscriptionSendLogEntity`
- Scheduled jobs configured in `JobInitializer`

**Authentication**:
- Cookie authentication (default)
- Google OAuth integration
- Admin user identified via `Auth__AdminUserGoogleId` environment variable

**Observability Stack**:
- **Logging**: Serilog with Console, File, and Seq sinks
- **Metrics**: Prometheus exporter at `/metrics`
- **Tracing**: SerilogTracing with OpenTelemetry instrumentation
- **Health Checks**: `/healthz` endpoint
- **Monitoring**: Grafana dashboards consuming Prometheus data

### Frontend Stack

- **HTMX 2.0** - Server-driven interactions without writing JavaScript
- **Alpine.js** - Lightweight reactive UI components
- **TailwindCSS + DaisyUI** - Utility-first CSS with component library
- **Highlight.js** - Code syntax highlighting (including Razor support via highlightjs-cshtml-razor)
- **EasyMDE** - Markdown editor (CodeMirror-based)
- **Mermaid** - Diagram rendering

**Asset Build Pipeline**:
- Webpack bundles JavaScript (`src/js/main.js` → `wwwroot/js/dist/`)
- PostCSS processes TailwindCSS (`src/css/main.css` → `wwwroot/css/dist/main.css`)
- Babel transpilation for browser compatibility
- Terser minification in production

**JavaScript Modules**:
- `typeahead.js` - Search autocomplete
- `translations.js` - Translation UI
- `simplemde_editor.js` - Markdown editor setup
- `comments.js` - Comment interactions
- `mermaid_theme_switch.js` - Theme switching for diagrams
- All exposed on `window.mostlylucid` namespace

### Configuration

**Key Settings** (appsettings.json):
- `Blog.Mode` - "File" or "Database"
- `Markdown.MarkdownPath` - Path to markdown files
- `TranslateService.Enabled` - Enable/disable auto-translation
- `TranslateService.ServiceIPs` - Translation API endpoints
- `TranslateService.Languages` - Target languages for translation
- `Analytics.UmamiPath` - Umami analytics URL
- `ConnectionStrings.DefaultConnection` - PostgreSQL connection string

**Environment Variables** (production):
- Use `.env` file for Docker deployment
- Critical settings: Google OAuth credentials, SMTP credentials, database connection, API keys

### Database Schema

**PostgreSQL with Full-Text Search**:
- Schema name: `mostlylucid`
- Full-text search column: `SearchVector` (computed `tsvector` with GIN index)
- Search query: `to_tsquery('english', search_term)`

**Key Entities**:
- `BlogPostEntity` - Title, Slug, Markdown, HtmlContent, PlainTextContent, ContentHash, PublishedDate, Categories (many-to-many), Language (foreign key)
- `CategoryEntity` - Name, many-to-many with BlogPosts
- `LanguageEntity` - Name, one-to-many with BlogPosts
- `CommentEntity` - Markdown content, Author, Status, BlogPost (foreign key)
- `CommentClosure` - Closure table for nested comment hierarchy

## Development Workflow

### Adding a New Blog Post (File Mode)

1. Create markdown file in `Mostlylucid/Markdown/` with naming: `{slug}.md`
2. File structure:
   ```markdown
   # Title
   <!-- category -- Category1, Category2 -->
   <datetime class="hidden">2025-01-15T12:00</datetime>

   Content here...
   ```
3. File watcher automatically processes and saves to database
4. Translation service auto-translates to configured languages

### Adding a New Controller

1. Create in `Mostlylucid/Controllers/`
2. Inherit from `BaseController` for shared functionality
3. Inject `ILogger<T>` and `BaseControllerService` for common operations
4. Use `IActionResult` return types

### Adding a New Service

1. Define interface in appropriate project (usually `Mostlylucid.Services`)
2. Implement service class
3. Register in `Program.cs` DI container with appropriate lifetime (Scoped/Singleton/Transient)
4. Follow repository pattern for database access

### Frontend Development

1. Edit source files in `Mostlylucid/src/css/` or `Mostlylucid/src/js/`
2. Run `npm run watch` for automatic rebuilding
3. Changes appear in `wwwroot/css/dist/` and `wwwroot/js/dist/`
4. TailwindCSS classes scanned from `**/*.cshtml` views and email templates

### Testing

- Use xUnit for test framework
- Moq for mocking dependencies
- `MockQueryable.Moq` for mocking EF Core DbSet queries
- Test structure mirrors source structure

## Important Patterns & Conventions

### Dependency Injection

- Use constructor injection
- Register services in `Program.cs`
- Strategy pattern for blog mode selection (File vs Database)
- POCO configuration binding via custom `ConfigurePOCO<T>` extension

### Error Handling

- Centralized exception handling via middleware
- Serilog structured logging for all errors
- Health checks for critical dependencies

### Security

- Antiforgery tokens with custom header `X-CSRF-TOKEN`
- HTTPS redirection enforced
- User secrets for development (User Secrets ID: `c720973b-30fe-465d-a96f-6c9923332a29`)
- Environment variables for production secrets

### Content Security

- ImageSharp processes all images with file system caching (`wwwroot/cache/`)
- Article images stored in `wwwroot/articleimages/`
- User uploads in `wwwroot/uploads/`

### Markdown Metadata Extraction

When processing markdown files, metadata is extracted via specific patterns:
- **Categories**: `<!-- category -- Cat1, Cat2 -->`
- **Published Date**: `<datetime class="hidden">2025-01-15T12:00</datetime>`
- **Title**: First `# Heading` in the file

### Umami Analytics Integration

- Custom `Umami.Net` client library (also published as NuGet package)
- Background sender queues events to avoid blocking requests
- JWT response decoding for analytics data retrieval
- Bot detection to filter out non-human traffic

## Infrastructure

### Container Services (docker-compose.yml / podman-compose.yml)

This platform supports both Docker and Podman for running containerized services:

**Services:**
- **mostlylucid** - Main web app (scottgal/mostlylucid:latest)
- **db** - PostgreSQL 16 (port 5266 externally)
- **umami** - Umami analytics
- **easynmt** - Translation API (EasyNMT)
- **caddy** - Reverse proxy and HTTPS termination
- **seq** - Structured log aggregation
- **prometheus** - Metrics collection
- **grafana** - Metrics visualization
- **watchtower** - Automatic container updates (Docker only)
- **cloudflared** - Cloudflare tunnel
- **node_exporter** - Host metrics

**Podman-Specific Notes:**
- Watchtower is replaced by `podman auto-update` with systemd timers
- Volume mounts use `:Z` suffix for SELinux labeling
- Rootless operation by default for enhanced security
- See `PODMAN.md` for complete Podman migration guide
- See `Mostlylucid/Markdown/DOCKER-VS-PODMAN.md` for detailed comparison article

### Monitoring Endpoints

- `/healthz` - Health check endpoint
- `/metrics` - Prometheus metrics
- `/dashboard` - Hangfire dashboard (scheduler service)
- Swagger UI available in development

## Common Gotchas

- **Markdown file changes**: File watcher only works when app is running; restart may be needed if files changed while stopped
- **Translation hash files**: `.hash` files prevent re-translation; delete to force re-translation
- **Database mode**: Even in database mode, the file watcher syncs files to DB
- **Frontend builds**: Always run `npm run build` before deploying; Docker build may not include node_modules
- **Entity Framework**: Migrations must be created with both `--project` (DbContext) and `--startup-project` (Mostlylucid) flags
- **Connection strings**: PostgreSQL connection string format differs between EF Core and Hangfire; check appsettings.json examples
