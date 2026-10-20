# Image-only upgrade verification — 20 October 2026

The existing `release-*` → Docker Hub `:latest` → Watchtower deployment remains
unchanged. The website image now includes the résumé compiler, its complete
public career record, downloads and default site fragments. Markdown overrides
are optional. No Compose edit, new service, port, environment variable or manual
migration is introduced. See [deployment and upload paths](website-deployment.md).

## Baselines checked

The latest GitHub Release belongs to another tool in this repository. Website
baselines were identified from website tags, the Docker publishing workflow and
Docker Hub instead:

- Last website tag: `release-august2026-03`, commit
  `c862265e8b2a5478c85bb5ac32458068774d0787`.
- Last published website workflow: [run 36361049789](https://github.com/scottgal/mostlylucidweb/actions/runs/36361049789),
  commit `2cb5ffd10d44273843df677e268e2651c7d85358`, 28 September 2026.
- Actual published Linux amd64 image pulled and run:
  `scottgal/mostlylucid@sha256:d083783b56ffed7dfc60878bb2c493005fbcc4bbe6f2b59dc932837f11da93be`.

Entity definitions and migration files are unchanged relative to both baselines.
The local candidate is `mostlylucid:ordering-fixed`; it has not been published.

## Rehearsal results

An isolated PostgreSQL 16 database and writable copies of existing-style content,
image, upload, model and data mounts were used. The actual previous image served
an existing article and approved comment before replacement. Only the image was
changed; the database, configuration and mounts stayed the same.

The candidate passed:

- Home, `/blog`, existing article, search, `/resumes`, `/resume` and health requests.
- Existing Markdown section override, legacy résumé upload and shared compiler
  publication preserved. Article and comment counts remained one each.
- Database schema dump unchanged; all 18 migration history entries retained.
- CSS and JavaScript content-version URLs changed, so previously cached asset
  URLs do not pin visitors to the old interface.
- Old protected login cookie rejected even when renamed to the new cookie name.
  Existing users sign in again once.
- Complete static PDF/Word/Markdown downloads and compiler-generated downloads;
  PDF and Word file signatures checked.
- Two independent visitors compiled successfully after warming the form cache.
  Compiler responses use `Cache-Control: no-store`, protecting antiforgery tokens
  and ensuring uploaded record changes become visible promptly.
- Valid Markdown career-record upload published; malformed and empty uploads
  kept the last good record; deleting the override restored the image default.
- Container restart preserved the record and shareable publication; deliberately
  terminating the compiler triggered automatic recovery.
- Rollback to the actual previous image served the existing article and upload;
  the candidate was then restored against the same database.

The exact packaged-image smoke check added to the workflow was also run locally.
It must pass **before** the workflow pushes either Docker Hub tag.

## Local checks

- Docker build: 230 tests passed, 8 skipped, zero failures. Five opt-in PostgreSQL
  cases among the skips were subsequently run against a real isolated database.
- Targeted run: all 5 PostgreSQL regression cases and 14 authentication cases
  passed. SQL cases cover filters, dates, semantic fusion, fallback order and
  category-selector query translation.
- Front-end: 6 Vitest checks passed; the delayed-search/filter browser regression
  passed against the final container.
- Final navigation audit: 115 destinations checked, article bodies excluded.
  Two article destinations are absent from the small local content fixture;
  both returned HTTP 200 on the live website. npm's package page requires a
  manual browser check because it blocks the audit client.
- Final home page visually inspected in a browser; final image runs locally at
  `http://localhost:8080` with the embedded compiler and no external compiler URL.

## Boundaries

This is a representative existing-schema rehearsal, not a copy of production
or a deployment. The fixture was created from the unchanged EF model with the
released migration history recorded, then verified against the actual old
image. A separate fresh-empty-database attempt exposed an existing historical
migration failure involving `MarkdownFetches`; fresh database installation is
not validated by this upgrade rehearsal. No migration was added or changed.

Production Google sign-in cannot be exercised using local dummy OAuth settings.
Token validation, authorization and cookie rejection are covered locally.
The browser's blocked analytics request points to a deliberately unavailable
local analytics placeholder.

Optional uploads use `/mnt/markdown/site/*.md`; generated compiler state and
shared résumés use `/mnt/markdown/site/resume-data/` on the existing writable
Markdown mount. Uploads to the compiler record do not regenerate the static
complete résumé downloads; regenerate those in the repository for a new image.

## Article ordering follow-up

Default ordering is newest publication first on home, blog, category and search
pages. Pinned metadata no longer overrides the chosen order. Sorting is applied
before pagination, with a stable tie-breaker, to prevent page-one pin promotion
from duplicating or skipping articles on later pages. Search's server, filter
selector and browser state now agree on the newest-first default; explicitly
selected title, oldest-first or relevance sorting remains available.

Regression checks cover older pins versus newer unpinned posts, pagination in
both database and Markdown modes, category pagination and search filter defaults.
