# Interactive article explainers

Research date: 2026-10-23. The implementation lives in `Mostlylucid/src/js/explainers` and `Mostlylucid/wwwroot/interactive/explainers`. The storyboards below describe the intended direction; current scope is a deterministic three-feature centroid replay, synthetic allowed/challenged proxy traces, and a destination replay captured from the C# prediction engine. Ordinary Mermaid fences retain their existing renderer. Pele support in the enhancements package is an unreleased source change.

## Recommendation

Use **Pele for process/architecture diagrams** and **deck.gl for the vector-space explorer**. Share a small plain-JavaScript playback controller, captions, evidence inspector and theme tokens. Both can live alongside the existing Alpine/HTMX site without introducing React.

The purpose is to let readers change an observation and see the consequences. A moving point or packet needs an accompanying explanation of what changed and why.

## Renderer research

| Candidate | Best use here | Decision |
|-----------|---------------|----------|
| [Pele](https://pele.run/) | Mermaid-authored flow, sequence and state diagrams; themeable SVG | First choice for process diagrams. Synchronous render/mount, CSS variables and responsive layout. |
| [beautiful-mermaid](https://github.com/lukilabs/beautiful-mermaid) | Polished themed SVG for its supported diagram types | Alternative to compare visually. Its documented coverage is narrower than Pele's; it is a separate renderer/package. |
| [deck.gl](https://deck.gl/docs/api-reference/core/orbit-view) | Interactive vector clouds in 2D/3D, selectable points and layered annotations | First choice for embedding-space exploration. Use non-geographic coordinates, an orthographic view for the initial teaching mode, and optional orbit view. |
| [regl-scatterplot](https://github.com/flekschas/regl-scatterplot) | Dedicated high-performance 2D scatter display with selection | Strong alternative if we decide to support only 2D. Its published capacity claims are not benchmarks of our widget. |
| [Sigma.js](https://www.sigmajs.org/) | Exploring an explicit community/similarity graph with many edges | Later option when graph connectivity itself is the subject. |
| [D3](https://d3js.org/) | Bespoke axes, transitions and interactions | Useful toolbox if required; not an additional default dependency when Pele and deck.gl cover the first views. |

Qdrant's current UI uses its own [scatter component](https://github.com/qdrant/qdrant-web-ui/tree/master/src/components/VisualizeChart) and a [worker](https://github.com/qdrant/qdrant-web-ui/blob/master/src/components/VisualizeChart/worker.js) for UMAP/t-SNE/PCA-related layout. We want its approachable exploration experience, not a dependency on its dashboard or a vector database for the article demo.

## Pele compatibility and theming

Pele is likely the newly released renderer the discussion refers to. [Obsidian 1.15's release notes](https://obsidian.md/changelog/2026-10-22-desktop-v1.15.0/) describe the adoption. Its [API](https://pele.run/api) provides synchronous `render()` and responsive `mount()`, and supports per-type imports/lazy loading.

The [theming API](https://pele.run/theming) exposes background, foreground, surface, border, accent, font and radius variables. Map these to the article's existing light/dark tokens. Use the same category colours and semantic highlight colours in the point cloud.

Syntax support does not guarantee identical integration. [Compatibility notes](https://pele.run/compatibility) describe different SVG classes/layout, lack of arbitrary HTML labels and ignored JavaScript click callbacks. Existing Mermaid theme variables also do not apply. The site's `@mostlylucid/mermaid-enhancements` integration and HTMX reinitialisation need explicit qualification.

Start with isolated explainer mounts. Keep the current Mermaid renderer for ordinary article fences during evaluation. Compare representative flowcharts, sequences, subgraphs, maths and existing controls before considering a site-wide change. Pele's publisher reports performance benefits; our content needs its own measurement.

## Storyboard 1: How observations become recommendations

Place near the first centroid explanation in [Part 1](../../Mostlylucid/Markdown/show-me-what-you-got-streaming-recommendations.md).

1. **Choose the coordinates.** Start with two named teaching dimensions and literal values. Hover/select a point to see its values and source. Category colours are explicitly illustrative labels.
2. **Summarise observations.** Select several points and reveal their weighted centroid. Adjust one event's weight and show the arithmetic beside the moving centroid.
3. **Receive another observation.** Step through a deterministic session. Show the new contribution, time decay, updated mass and current summary. Play/pause/reset/scrub share the same event state.
4. **Find candidates.** Highlight neighbours using the chosen distance. Show the actual coordinates/distances and a corresponding ranked list.
5. **Apply constraints.** Toggle availability or budget. Ineligible candidates remain visible but dimmed; eligible retrieval is recomputed before top-K.
6. **Discover groups, later.** Show several interest populations and a similarity graph only when explaining communities. Label synthetic groups separately from actual algorithm outputs. Leiden is not implemented in the current core.

Default to a legible 2D view. Optional 3D should help explain a third dimension and provide familiar orbit/zoom controls. Use known teaching coordinates first; the Qdrant-style projected cloud is a subsequent mode for real high-dimensional examples.

Projection is a display operation, not the recommendation metric. Compute centroids and distances in the source space. A fixed PCA transform is a useful first projection because it can be applied consistently to new summaries. Freeze the projection during replay: refitting it can make stationary points appear to move. UMAP/t-SNE views must not imply that every displayed distance, gap or apparent cluster is preserved. Different feature heads have distinct spaces; the display must name the active representation.

## Storyboard 2: What a protective reverse proxy does

Use the topology described in the [Stylo.Bot gateway article](../../Mostlylucid/Markdown/botdetection-part3-as-simple-as-possible.md). The illustrated path is client → TLS edge → gateway → origin, with the response returning through the path. Explain transparent operation as requiring no application SDK, rather than suggesting the gateway can inspect arbitrary encrypted traffic without an appropriate termination point.

A sequence/process view should expose:

1. An incoming request with a small selectable envelope.
2. Observations extracted at the gateway, with provenance and availability.
3. Evidence accumulation and a versioned policy evaluation.
4. An allowed request reaching the origin, a challenge handled at the gateway, or another configured policy path.
5. The origin response returning to the client where applicable.
6. Asynchronous telemetry/profile work shown on a separate lane, so background work is distinguishable from the request's waiting time.

Offer labelled synthetic scenarios: ordinary browser, known crawler, unfamiliar client, and scripted burst. Select a stage to inspect its evidence and reason; stepping should make early exits and origin reachability obvious. The first version is an educational replay, not live proxy telemetry or a claim to reproduce every detector.

Use Pele to establish the SVG layout. Application-owned highlighting and request markers advance through a deterministic event sequence. Keep positions stable during playback; rebuild layout only for topology changes or an explicit responsive reset. Respect reduced-motion preferences and expose equivalent step descriptions and a table.

## Shared component boundary

A small controller can accept scenario data with versioned entities, observations and step snapshots. Views subscribe to step changes; the inspector shows the same selected entity/evidence. Keep deterministic replay separate from rendering, with no wall-clock randomness changing the result after reset.

This can later illustrate the Part 1.5 classifier bank, a summariser's analysis waves or an intervention comparison. A reusable component contract is more valuable than making every concept use one visual layout.

Prefer a lazy-loaded, same-origin standalone explainer page with an article preview/fallback and full-screen link. An isolated iframe is an option for CSS/WebGL lifecycle containment; verify the Markdown rendering path and implement height/mount cleanup explicitly. Avoid adding a large WebGL bundle to every blog page. Produce a static SVG/image plus textual explanation for feeds, print and unavailable JavaScript.

## First implementation and validation

Build the 2D centroid replay and the proxy's allowed/challenged request replay first. Validate correct arithmetic, eligibility-before-top-K, deterministic reset and no projection refitting. Then add the 3D cloud and real-engine replay data where it helps.

Check keyboard controls, touch selection, narrow-screen layout, light/dark mode, reduced motion, full-screen inspection, export/fallback and cleanup after HTMX navigation. Capture desktop/mobile views. Measure cold-load bundle cost, frame time and layout cost on the actual scenarios before describing the experience as high performance.


## First implementation evidence (2026-10-23)

- Enhancements package: 38 tests passed, including injected renderer/source preservation and passive touch/explicit activation/real disable API regression coverage.
- Site: 23 focused tests passed (17 existing Mermaid integration tests and 6 replay/ranking tests).
- Production Webpack build and package TypeScript/minified/Rollup builds passed. Existing bundle-size and module-format warnings remain; no renderer performance benchmark is claimed.
- Browser checks: allowed requests reach the origin, challenge requests do not; keyboard inspection works after changing scenario; budget/category filters return eligible candidates; 2D and 3D views render.
- Browser touch emulation: a swipe advanced page scroll from 434 to 685 pixels with identical SVG viewport transforms. A tap left interaction disabled. The explicit toolbar enables and disables interaction.
- Dark fullscreen and SVG download passed. The dry-run npm package includes the adapter source, compiled JavaScript and declaration files. Nothing was published to npm.
- Mobile embed measured 1639 pixels for 1634.7 pixels of content, then shrank to 1056 pixels at desktop width. Parent theme propagation passed; mobile layout had no horizontal overflow.
- With WebGL deliberately unavailable, the SVG fallback displayed 144 candidates and the four ranked results; the fallback remained visible in print mode.
- Desktop article screenshots are checked in under `Mostlylucid/wwwroot/articleimages/explainer-*.png`; additional mobile/3D inspection screenshots remain under ignored `output/playwright/`.

These are local checks in Chromium, including touch emulation, rather than a physical-device matrix or a deployed-site verification. The Docker build context now copies the shared renderer source, but a complete container build was not run for this frontend change.

## Part 3 behavioural tracks (2026-10-24)

The `prediction` view shows 24 individual behaviour tracks: three teaching paths and 21 generated holdout journeys. `PredictionReplay.cs` captures actual prefix forecasts against frozen training counts; the browser imports that artifact directly. Individual trails remain visible, moving heads interpolate between observations, and starts are staggered. The replay starts automatically when visible, loops, and pauses for background/offscreen views. Reduced-motion users start with a static populated view and explicit controls.

The spatial layout is an authored arrangement of named behavioural states, with stable per-user display offsets. It does not derive coordinates from outcome probabilities or future labels. Basket removal and post-cart browsing visibly move backwards; checkout approaches the purchase region; observed endpoints enter their corresponding destination bands. Incomplete visits stop at their last observed state. Forecasts, unresolved mass and utility remain available in the inspector and data table. Changing the objective does not alter an observed track.

Shared play/pause/reset/scrub controls, touch scrolling, SVG fallback and print support remain. The initial probability-anchor display was replaced because coincident prefixes hid the individual journeys and did not communicate the behaviour tracks.

Validation for this replacement: 53 frontend tests and 59 C# tests passed. Browser assertions counted 24 distinct head elements and 24 trails, then verified at least 12 heads changed position during a controlled playback interval. At 390px the view has no horizontal overflow. The checked-in snapshot now shows the field of diverging tracks rather than the probability-anchor display.

## Physical-time trajectory view

The prediction view now imports `temporal-shop-replay.json` and `temporal-operations-replay.json`, captured from the actual elapsed-time predictor. The UTC playhead advances recorded seconds, with 1×/10×/30×/60× playback. Head positions project the first two converter-defined centroid coordinates; small identity offsets preserve selection. Probability inference uses the source centroid, velocity, acceleration, elapsed time and learned outcome populations, not the display offsets or screen distances.

Compare direction pins A/B at identical raw input vectors with opposite rates; compare pace pins A/C at their own landmarks, reached after 20 and 120 seconds. Both cards show real forecasts for the same 60-second target. The service view displays a separate learned normal reference and deviation; only qualified healthy samples update it. Playback interpolation is not an additional sensor measurement. Recorded forecast windows that have elapsed are labelled explicitly.

Comparison cards and motion evidence exceed the original 2,400px mobile iframe bound. The source/origin-checked resizing handler now admits up to 8,000px, allowing comparison and accessible-table expansion without a nested scrolling viewport.

The prediction player provides a Separate tracks control. Fixed vertical display lanes distinguish coincident trajectories; turning it off restores the exact feature projection. Lane spacing never enters forecasting. Compare labels use opposing offsets so two highlighted heads remain readable.

Article math uses `$$...$$` for display equations and `$...$` for inline expressions. The Markdown parser preserves these before KaTeX produces HTML and MathML. Backslash bracket/parenthesis delimiters are consumed as Markdown escapes and must not be used in article source. Check the rendered article, including subscripts, in a local browser.
