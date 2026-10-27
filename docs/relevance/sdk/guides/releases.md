# Packaging and releases

The `Relevance packages and Native AOT` workflow verifies the package set, builds complete native assets, generates SDK documentation, assembles a relocatable feed and runs an application against that feed. A normal push or manual verification run does not publish to NuGet.org.

## Verification artifacts

The framework feed contains Mostlylucid.Relevance, Mostlylucid.Relevance.Sqlite, Mostlylucid.Relevance.Learning and Mostlylucid.Sqlite.Vector. The native version tracks its pinned wrapper separately from the core preview version. `NuGet.Config` points at the extracted feed directory and NuGet.org for transitive dependencies. Restore with that config; consumers do not compile Rust or manually copy native libraries.

Generated API Markdown and usage guides ship in the packages. The SDK HTML site is a separate workflow artifact and is also included in the release feed. `AGENTS.md` is supplied explicitly; NuGet does not automatically insert it into an agent's context.

## Publish gate

The workflow's manual `publish` input is an explicit opt-in. Publishing depends on the successful documentation, matrix tests, AOT package consumers and final feed consumer. The publisher downloads those exact verified packages and uses NuGet Trusted Publishing/OIDC as user `mostlylucid`.

Configure a NuGet.org trusted-publisher policy for this repository and workflow filename, scoped to these package IDs. The repository's other package-specific API key is not reused. Missing policy is an external publishing prerequisite and the job reports the login failure rather than pretending the release succeeded.

Package versions are taken from the committed project files. Bump the appropriate version and the package consumer's references together before a new immutable release. Already published versions are not silently overwritten. Publication is additive; verify version/IDs and release evidence before selecting publish.

## Local docs and AOT checks

```sh
dotnet tool restore
python scripts/relevance/build-sdk-docs.py
# Open docs/relevance/sdk/_site/index.html through a local HTTP server.
dotnet publish samples/RelevancePackage -c Release -r osx-arm64
```

API references are generated from the four projects' public declarations and XML comments; a coverage check detects documented members/types missing from the export. The build fails on documentation warnings. The native package's release pack requires all declared runtime assets; a host-only partial pack is an explicit local-test mode and never the final published set.
