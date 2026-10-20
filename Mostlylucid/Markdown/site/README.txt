Editable site areas
===================
These files are page fragments, not blog posts. They are never imported, translated,
or indexed. All paths below are relative to Mostlylucid/Markdown/site/.

  resume-callout.md   Slim callout ABOVE the header on every page. The first paragraph
                      is the message; the second contains the destination link.
                      Empty the file to hide the callout.
  home-intro.md       Home page introduction. Use one # heading, then short paragraphs.
                      Delete it to use the built-in introduction.
  current-projects.md Home page sidebar cards. The first card is the lead project. Use ## for the section title and a short
                      paragraph for the introduction. Each blockquote is a card:

                      > ### [Project name](/destination)
                      >
                      > A short description of the project.
                      >
                      > [Explore project →](/destination)

                      Separate cards with an HTML comment (<!-- project -->) on its
                      own line so Markdown treats them as separate blockquotes.
                      Cards stack in the sidebar; empty the file to hide them.
  resumes.md          Content at /resumes: résumé downloads, lucidRESUME, and contact.
                      The tool itself remains at /resume; this file edits its introduction.
                      Complete downloads are served from wwwroot/resumes/ at /resumes/.
  announcement.md     Dismissible announcement BELOW the header. Shown when there is
                      no active database announcement. Empty the file to hide it.
                      Editing it re-shows it to people who dismissed the old text.

Ordinary Markdown headings, paragraphs, emphasis, and links work in all files.
Keep links to the résumé/tool as normal links: the tool has its own application shell.
Content here is trusted site-owner content and may contain HTML.

The service picks up file changes on the next uncached request. In database mode,
the Markdown watcher also evicts the tagged home page cache when these files change.
Existing rendered/cached pages elsewhere refresh on their normal cache expiry.
No front-end rebuild is needed for text edits.

Complete résumé exports
-----------------------
  resume-complete.md  Human-readable source for the COMPLETE downloadable résumé.
                      It is separate from resumes.md (the résumé landing page).

After changing resume-complete.md, rebuild the checked-in PDF, Word and Markdown
assets in Mostlylucid/wwwroot/resumes/:

  python3 -m venv /tmp/mostlylucid-resume-export
  /tmp/mostlylucid-resume-export/bin/pip install -r scripts/resume-export-requirements.txt
  /tmp/mostlylucid-resume-export/bin/python scripts/export-complete-resume.py

Run these commands from the repository root. Review the generated documents before
publishing. Keep dates and achievements faithful to the career record. Existing
/uploads/SimpleResume.* files are historical; new UI links use /resumes/ instead.

GitHub activity widget
----------------------
The project sidebar loads /projects/activity separately from the article list.
GitHubActivityService reads public scottgal repositories ordered by last push,
excluding forks and archived repositories. It checks each displayed repository's
public releases before showing a shields.io release badge linked to /releases.
Results are cached for 30 minutes; failures retry after 5 minutes and retain the
last successful result. It uses no token and never exposes private repositories.

Each curated project has separate Articles and Repo links. Articles use existing
blog tags through /blog?category=TAG: Bot%20Detection, StyloAgent, StyloMail, and JobML
(for lucidRESUME). Edit those links in current-projects.md to change a mapping.

Docker deployment
-----------------
The image includes default fragments in /app/site-content/. Files in the mounted
/mnt/markdown/site/ directory override those defaults. Empty an override to hide a
section; deleting an override restores the image default. Downloads ship in the
image at /app/wwwroot/resumes/. See docs/website-deployment.md for the complete
image upgrade and upload instructions. The compiler is bundled in the website image.

  resume-career-record.md  Generated JobML version of resume-complete.md. The bundled
                          compiler uses this default immediately. Upload an override
                          to /mnt/markdown/site/resume-career-record.md to update the
                          compiler record without rebuilding. Changes are validated
                          within five seconds; invalid uploads keep the last good record.

Compiler snapshots and shareable résumés persist automatically under the existing
Markdown mount at /mnt/markdown/site/resume-data/. Keep that directory across updates.
The existing Google admin account can publish records; no new environment variable
is required. The résumé exporter also regenerates resume-career-record.md.
