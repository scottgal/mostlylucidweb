# Website upgrades: one image, existing Compose deployment

Push a `release-*` tag after committing the reviewed changes. The existing Docker
Image CI workflow builds and publishes `scottgal/mostlylucid:latest`; Watchtower
replaces the website container on its next check (configured for five minutes).
A `local-*` tag publishes `:local`, which production does not use.

**No Compose edits, new service, extra host port, environment variables, model
installation or manual database migration are required.** The image contains the
website, static résumé downloads, default editable sections, a pinned lucidRESUME
compiler and its complete public career record.

```sh
# After the reviewed changes have been committed and pushed:
git tag release-website-refresh-2026-10-20
git push origin release-website-refresh-2026-10-20
```

Use a new, unique tag. The workflow builds that commit. Do not tag an older commit
or expect uncommitted files to be included. No image has been published by these
local checks.

## What ships, and where uploads go

| Content | In the image | Optional host upload/override |
| --- | --- | --- |
| Website code, CSS, JavaScript and views | `/app` | None |
| Intro | `/app/site-content/home-intro.md` | `/mnt/markdown/site/home-intro.md` |
| Projects and their article/repository links | `/app/site-content/current-projects.md` | `/mnt/markdown/site/current-projects.md` |
| Callout above navigation | `/app/site-content/resume-callout.md` | `/mnt/markdown/site/resume-callout.md` |
| Résumé landing-page explanation/links | `/app/site-content/resumes.md` | `/mnt/markdown/site/resumes.md` |
| Complete PDF, Word and Markdown downloads | `/app/wwwroot/resumes/Scott-Galloway-Complete-Resume.*` | None needed |
| Complete public compiler career record | `/app/site-content/resume-career-record.md` | `/mnt/markdown/site/resume-career-record.md` |
| Blog articles | Existing external content | `/mnt/markdown/*.md` (unchanged) |
| Article images | Existing external content | `/mnt/articleimages/` (unchanged) |

These host paths are from the existing Compose mounts. The runtime uses the
configured Markdown path, so the same scheme applies if your deployment uses a
different host source path.

Existing Markdown overrides win over bundled defaults. Empty a section override
to hide it; remove it to restore the image default. Text edits require no rebuild.
Review old `/mnt/markdown/site/` files if the new image still shows older text.

The compiler career record is JobML Markdown, generated from the reviewed human
résumé. Upload `resume-career-record.md`, rather than the plain `resume-complete.md`,
to update the compiler. The compiler checks for changes every five seconds,
validates the evidence and publishes a new snapshot. A malformed/partial upload
keeps the last good record; a fresh installation falls back to the bundled record.

To edit the full résumé in the repository, change
`Mostlylucid/Markdown/site/resume-complete.md` and run the exporter documented in
`Mostlylucid/Markdown/site/README.txt`. It generates PDF, Word, Markdown and the
compiler career record. Include those outputs in the next image release. Static
complete downloads do not regenerate when an external override is uploaded.

## Compiler and storage

The website supervises the bundled compiler on `127.0.0.1:5098`, proxies `/resume`
to it, restarts it after unexpected exits, and stops it with the website. It is
not exposed as another container or host port. Compilation uses deterministic
matching and lexical fallback; it does not download or require an AI model.

Runtime data uses the **existing writable Markdown mount**:

- `/mnt/markdown/site/resume-data/jobml/`: published career snapshots;
- `/mnt/markdown/site/resume-data/publications/`: persistent shareable résumés;
- `/mnt/markdown/site/resume-data/source.sha256`: source update tracking.

Preserve `resume-data/` with the rest of the Markdown directory. Image replacement
and container restart retain this data. A record published through the owner UI
is retained unless the source Markdown changes.

The existing `Auth__AdminUserGoogleId` account may publish career records. Visitors
can compile from the published record; other users cannot replace it. Existing
Google client ID/secret settings continue to apply. If an external compiler is
explicitly configured through `LucidResumeProxy__UpstreamUri`, that configuration
continues to take precedence.

Authentication now validates Google ID tokens. Its cookie scheme and encryption
purpose have changed: **existing users must sign in again once**. Renaming an old
cookie does not make it valid. There are no database entity or migration changes
relative to either the last published website image or the last `release-*` tag.

## Checks after Watchtower updates

```sh
curl -f https://mostlylucid.net/healthz
curl -f https://mostlylucid.net/blog
curl -f https://mostlylucid.net/resumes
curl -f https://mostlylucid.net/resume/api/status
curl -f https://mostlylucid.net/resumes/Scott-Galloway-Complete-Resume.pdf -o /tmp/resume.pdf
python3 scripts/audit-site-links.py --base https://mostlylucid.net --report /tmp/mostlylucid-links.json
```

The compiler status should report `ready: true` without an upload. In a browser,
compile a job description and download Word/PDF; check search filters, newest-first
no-match suggestions and 404 suggestions. Sign in again with your Google account
if you use the writer UI. Local dummy OAuth settings cannot verify an actual
production Google account sign-in.

The workflow also publishes timestamped image tags for rollback. Record the old
image digest before release. Because schema and existing mounts are unchanged,
rolling back the image does not require reverting a database migration. New
compiler data lives in its own subdirectory, which older website images ignore.
See [upgrade verification](upgrade-verification.md) and
[regression test commands](review-regression-tests.md).
