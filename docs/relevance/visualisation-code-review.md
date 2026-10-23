# Visualisation review and test coverage

Reviewed 2026-10-23. Scope: the new article explainers, shared Mermaid/Pele enhancements, build/package entry points and their integration with the ASP.NET article renderer. The site-update article is the primary integration fixture; the package compatibility lab supplies isolated configuration cases.

## Findings fixed

| Area | Failure | Correction |
|---|---|---|
| Article embeds | A directory URL worked in Python's static server but returned 404 in ASP.NET. | Use explicit `index.html` URLs in all three articles. |
| Standalone navigation | Inherited HTMX boost inserted the standalone page into the article shell. | Explicitly disable boost on explorer links and check full-page navigation. |
| Mobile scroll chaining | Collapsed child margins left a five-pixel inner scroll range that could consume an entire touch gesture. | Make the embedded body a flow root and report its natural height. |
| Asset freshness | Standalone HTML lacked Razor asset versions; cached CSS could retain the scroll bug. | Revalidate explainer files, content-version JS/CSS during Webpack emission, and use a new iframe URL version to escape previously cached HTML. |
| Export geometry | svg-pan-zoom could put its viewport transform in CSS as well as an SVG attribute, clipping exported diagrams. | Strip both transforms from copies and restore source bounds. |
| Copy styling | Renamed SVG IDs invalidated Mermaid's embedded CSS selectors; HTML labels lost layout. | Rewrite stylesheet selectors and local marker URLs; preserve computed presentation and label layout. |
| Lifecycle | Delayed fit/open callbacks could execute after destruction, and some modal close routes leaked listeners. | Track/cancel timers, disconnect observers, centralize modal cleanup, and restore page gesture ownership. |
| Fullscreen access | Focus could leave the dialog; closing did not consistently restore the opener. | Add dialog semantics, Tab containment and focus restoration. |
| Reconfiguration | Custom icons were reset by unrelated configuration updates; a show button could retain a reference to an obsolete toolbar. | Merge current icons, recreate stale show buttons and resolve the current toolbar when showing it. |
| Toolbar APIs | Showing/toggling a slid-away toolbar could leave it clipped. Caller IDs were interpolated into selectors. | Normalize visibility state and compare ID values instead. |
| Forms | Toolbar buttons could submit a containing form. | Explicit `type="button"`. |
| Async rendering | Overlapping theme renders could finish in the wrong order; one failed adapter diagram stopped subsequent diagrams. | Serialize render batches, isolate per-diagram errors and preserve even empty original sources. |
| HTMX | Opt-in was ignored; root diagrams/code fences could be missed. History events could omit `detail.target`. | Honour opt-in, recognize both forms, and guard the site's history target. |
| Demo | Obsolete API imports prevented the old demo from starting; configuration cleanup did not rebuild controls. | Replace it with a local public-bundle compatibility lab and explicit rebuild actions. |
| Package | Root exports pointed at source TS/incorrect CommonJS entries; building the lab added renderer chunks to package contents. | Use built public bundles and restrict package contents to root distribution files. |
| Explainer state | Back/forward cache restoration could retain destroyed renderers; reset retained candidate detail; empty eligible results were described as missing evidence. | Restore lifecycle state, clear selection and distinguish evidence expiry from filter exhaustion. |

## Verification

- **Package:** all 57 tests pass, including modal close routes, timer cleanup, observer cleanup, empty-source recovery, async theme ordering, native SVG export, failed PNG cleanup, HTMX opt-in and touch activation.
- **Site:** all 47 frontend tests pass. This includes replay arithmetic, ranking boundaries, all four proxy traces, iframe message origin/source checks and theme propagation. Existing theme tests now check deferred startup events and CSS/global theme state rather than obsolete stylesheet switching.
- **Compatibility lab:** 34 automatic browser checks pass across Mermaid and Pele. Nine fixtures cover flow/subgraphs, sequence, class, state, entity relationships, pie, Gantt, mind map and normalized Markdown fences. Controls, grouped/granular presets, repeat init, passive taps, explicit gestures, hide/show, cleanup/recovery, dynamic content and final theme ordering are checked.
- **Real article:** three normal Markdown diagrams (flowchart, sequence, state) pass every toolbar control in both themes. Twelve downloads cover PNG/SVG for all three diagrams in both themes. The embedded Pele view passes its controls and four further light/dark downloads. Eight exported SVG files pass XML, local-marker and clean-viewport checks; representative PNGs were visually inspected.
- **Real mobile article:** at 390 × 844, swipes over both ordinary Mermaid and the embedded Pele diagram each advance the article by **200 pixels**, with unchanged diagram transforms. The iframe has equal viewport/document heights (**1916 pixels**), zero child scroll and no horizontal page overflow.
- **Navigation:** article → Contact → browser Back restores three toolbars and working zoom. Print switches diagram rendering to the light theme and restores the prior theme afterwards.
- **Builds:** production Webpack, TypeScript/minified/Rollup package builds and the ASP.NET project build pass. Existing compiler/bundle warnings remain.
- **Packaging:** dry-run contents exclude `dist/demo` and include the adapter, declarations and public bundles. Both bundle entry shapes were checked; CommonJS loading was exercised with a DOM. Nothing was published to npm.
- **Caching:** a fresh local build serves the explainer with `Cache-Control: no-cache`; its two asset-version values match the SHA-256-derived JS/CSS version. During preview, restart the old container after hot-copying assets: its response cache can otherwise serve older JavaScript even when the on-disk file changed.

## Repeat locally

From `mostlylucid-mermaid/`, run `npm ci`, `npm run dev:demo`, then the compatibility button and manual checklist. From `Mostlylucid/`, run `npm test -- --reporter=dot` and `npm run js:prod`. Build the site with `dotnet build Mostlylucid/Mostlylucid.csproj` from the repository root.

Use `/blog/site-update-interactive-explainers-pele-and-deckgl` in the ASP.NET preview, rather than only serving the static explainer directory. Inspect the ordinary code fences and both embedded views. The article includes instructions for controls, touch scrolling, fullscreen focus and independent file exports.

Browser verification used Chromium and touch emulation, not a physical iOS/Android device matrix. The manual checklist also covers physical pinch gestures, OS theme changes and reduced motion. Preview-only Google authentication/analytics/editor CDN warnings are outside this renderer review. The original preview container was restored after checking the freshly built host's cache headers.

A repeatable article-control check is saved in `mostlylucid-mermaid/examples/check-article.js`. With the article open in a Chromium Playwright CLI session, run from the repository root:

```bash
playwright-cli -s=article run-code "$(cat mostlylucid-mermaid/examples/check-article.js)"
```

It uses the actual article/iframe DOM, checks all three article diagrams in both themes, saves 16 downloads under ignored `output/playwright/`, exercises all four proxy policies, and asserts touch scroll chaining and zero inner iframe scroll range. Use the Codex Playwright wrapper instead of `playwright-cli` when that is how the browser session was opened.
