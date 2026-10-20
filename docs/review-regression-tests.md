# Review regression checks

Authentication, writer authorization and existing website tests:

```sh
dotnet test Mostlylucid.Test/Mostlylucid.Test.csproj --filter FullyQualifiedName~Authentication
```

The login tests reject a fabricated token through the real Google validator and
exercise validated claim mapping. Gateway tests run real HTTP requests against a
small local server with a fake compiler, covering the configured writer, another
account, an unauthenticated identity, and public compilation. Cookie migration
tests reject a valid legacy ticket even when replayed under the new cookie name.

PostgreSQL tests use real full-text search and semantic-result merging. They check
tag/date constraints, hidden and scheduled posts, newest-first no-match suggestions,
and inclusive end dates for both search and listings. They are skipped unless an
isolated database is explicitly configured. Every test rolls back its data.
The database name must start with `mostlylucid_review_`.

```sh
docker run -d --name mostlylucid-review-postgres -p 15439:5432 \
  -e POSTGRES_USER=regression -e POSTGRES_PASSWORD=local-regression-only \
  -e POSTGRES_DB=mostlylucid_review_fixes postgres:16-alpine
MOSTLYLUCID_TEST_POSTGRES='Host=localhost;Port=15439;Database=mostlylucid_review_fixes;Username=regression;Password=local-regression-only' \
  dotnet test Mostlylucid.Test/Mostlylucid.Test.csproj --filter FullyQualifiedName~PostgresSearchRegressionTests
docker rm -f mostlylucid-review-postgres
```

For the search race, run the site locally on port 8080 with Bot Detection articles
available, then use Playwright CLI:

```sh
playwright-cli open http://localhost:8080/search
playwright-cli run-code --filename scripts/check-search-race.js
```

The browser check delays automatic search, selects Bot Detection while that search
is pending, and asserts that the selected topic and URL survive the older response.
