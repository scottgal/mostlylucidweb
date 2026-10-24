# Website dependency upgrade, 24 October 2026

Scope: the deployed website, its referenced projects and tests, and the shared Mermaid enhancements used by the website. Historical article samples and unrelated applications are outside this release.

## Updated

- ASP.NET Core, EF Core and Microsoft.Extensions references aligned with current .NET 10 releases: 10.0.13; diagnostics packages 10.11.0.
- Markdig 1.5.0, OpenTelemetry 1.20.1 (existing preview-only exporters retain their matching beta/RC channel), Scalar 2.18.3.
- ONNX Runtime 1.31.0, Qdrant 1.20.0, DuckDB 1.5.6.1, AngleSharp 1.8.5 and NodaTime 3.3.6.
- Test SDK 18.11.0, Playwright 1.64.1, PuppeteerSharp 25.15.0, EF-compatible mocking packages and the test runner/coverage packages.
- Mermaid 12.2.0, TypeScript 7.0.2, Vitest 5.0.5, Happy DOM 21.0.4, jsdom 30.1.3, cssnano 9.9.0, Babel/Webpack/Rollup and other available frontend updates. deck.gl 9.4.0 and Pele 1.9.0 were already current.
- Lockfile transitives refreshed. Both npm dependency trees report zero known vulnerabilities. A `postcss-selector-parser` override selects patched 7.1.x while retaining the current CSS framework.

The website now consumes `@mostlylucid/mermaid-enhancements` from the local package. Its peer range includes the tested Mermaid 12 release. Docker copies that package manifest, its source, and the captured replay JSON needed by Webpack. No npm package is published by the website release.

TypeScript 7's native compiler no longer exposes the JavaScript compiler API expected by the old Rollup TypeScript plugin. `tsc` performs type checking and emits ESM/declarations; Rollup bundles that emitted ESM. Test setup accommodates current Happy DOM property/iframe behaviour.

## Deliberate holds

- Tailwind 3.4.19 and DaisyUI 4.12.24 are the latest compatible releases of the existing styling stack. Tailwind 4/DaisyUI 5 require a separate CSS migration and visual review.
- Node types follow the deployment's Node 22 major. The frontend minimum is Node 22.22.3, reflecting current dependency requirements; the Docker build installs current Node 22.
- ImageSharp 3.1.12 and ImageSharp.Web 3.2.0 retain the existing production versions. Newer versions tested here, including ImageSharp 3.2.0/4.1.3 and Web 4.0.2, enforce a Six Labors license key at Release build time. No such key is configured in the release workflow or repository secrets. Their upgrade remains pending license configuration and API migration. Existing ImageSharp NuGet security advisories remain visible; no audit warning was suppressed.

## Validation

- Website Release build passed. Existing compiler warnings and the ImageSharp advisory warnings remain.
- Website CI-filtered tests: 230 passed, 8 skipped, 0 failed.
- Website frontend tests: 53 passed. Shared renderer tests: 57 passed.
- Production CSS/JavaScript and shared TypeScript/minified/ESM/UMD builds passed. Existing bundle-size and module-format warnings remain.
- Chromium rendered the article's Mermaid diagram and 24 behavioural tracks with the updated frontend, without horizontal overflow at 390px.
- The Docker release smoke check verifies the explainer HTML, bundle and versioned replay data are packaged before image publication.

Blog articles remain externally uploaded content. The existing `.dockerignore` article exclusions are unchanged; this release ships their supporting application assets.
