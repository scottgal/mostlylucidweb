# Navigation audit — 20 October 2026

Scope: header/footer navigation, homepage project links, Software page links,
résumé downloads and compiler, search, feeds and repository activity. Historical
article body links were excluded, as requested.

The live audit checked 100 destinations using GET. Production still has the old
image; these failures are corrected in the working tree and require a release:

| Live destination | Observed | Change |
| --- | --- | --- |
| `/resumes` | 404 | New explanation page and complete PDF/Word/Markdown downloads |
| `/resume` | 404 | Compiler gateway; redirects to downloads when unconfigured |
| `/projects/activity` | 404 | New repository activity and release links |
| `stylo.bot` | 502 | Software page now links to `www.stylobot.net` |
| Eufy directory in mostlylucidweb | 404 | Links to the separate `scottgal/mostlylucid.eufysecurity` repository |
| Mermaid JavaScript source directory | 404 | Links to the existing `mostlylucid-mermaid` directory |
| PagingTagHelper directory | 404 | Links to the separate `scottgal/mostlylucid.pagingtaghelper` repository |
| MinimalBlog NuGet package | 404 | Removed unpublished package button/badge; retained working source link |

The scoped npm package page returns 403 to the automated checker and needs a
manual browser check. This is recorded as an unverified destination.
Cloudflare's email-obfuscation URL is a browser-decoded email link and is excluded
from HTTP destination checks.

Local verification (final website image: 214 tests passed, 3 skipped; six browser filter-state tests passed):

- No-match search explicitly identifies the fallback and returns newest articles
  first, even when the requested sort is oldest first. Hidden and scheduled
  posts are excluded. Date/language/tag filters remain in effect.
- A mistyped article path retains 404 and offers matching article links. A path
  with no close match offers recent articles. Following the top suggestion opens
  the intended article.
- HTMX navigation to a missing destination opens the complete 404 page, so its
  suggestions are visible rather than hidden behind a generic error toast.
- The final website image was smoke-tested with a mounted article directory and
  no site overrides: packaged sections and all three complete résumé downloads
  were served successfully. Its navigation audit checked 115 destinations.
- The website and Linux compiler containers compiled a job description over their
  private Docker network without an AI service;
  its generated Markdown, Word and PDF downloads all returned 200.
- Anonymous career-record changes return 401 at the website gateway.

The local preview uses a limited file-backed article collection. Two Software
page article destinations return 404 there: `docsummarizer-advanced-concepts`
(source absent) and `building-a-document-summarizer-with-rag` (existing malformed
source title). Both resolve on production; article cleanup remains separate from
this navigation work.

Re-run after publishing:

```sh
python3 scripts/audit-site-links.py --base https://mostlylucid.net --report /tmp/mostlylucid-links.json
```

See [deployment instructions](website-deployment.md) for image publication,
editable content overrides and the bundled compiler and Markdown override paths. Local checks do not
establish that production has received the release.
