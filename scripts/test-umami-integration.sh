#!/usr/bin/env bash
# Disposable, loopback-only Umami/PostgreSQL integration stack. No production configuration is read.
set -euo pipefail
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
stack="umami-sdk-test-$$"
server="$stack-server"
database="$stack-db"
cleanup() {
    result=$?
    if [ "$result" -ne 0 ]; then docker logs "$server" 2>&1 || true; docker logs "$database" 2>&1 || true; fi
    docker rm -fv "$server" "$database" >/dev/null 2>&1 || true
    docker network rm "$stack" >/dev/null 2>&1 || true
    exit "$result"
}
trap cleanup EXIT

docker network create "$stack" >/dev/null
docker run -d --name "$database" --network "$stack" \
    -e POSTGRES_USER=umami -e POSTGRES_PASSWORD=local-umami-only -e POSTGRES_DB=umami \
    postgres:16-alpine >/dev/null
ready=false
for attempt in $(seq 1 30); do
    if docker exec "$database" pg_isready -U umami -d umami >/dev/null 2>&1; then ready=true; break; fi
    sleep 1
done
if [ "$ready" != true ]; then echo 'Disposable PostgreSQL did not become ready.' >&2; exit 1; fi

docker run -d --name "$server" --network "$stack" -p 127.0.0.1:18086:3000 \
    -e "DATABASE_URL=postgresql://umami:local-umami-only@$database:5432/umami" \
    -e APP_SECRET=local-disposable-umami-sdk-test-secret \
    ghcr.io/umami-software/umami:3.4.0 >/dev/null
ready=false
for attempt in $(seq 1 90); do
    if curl -fsS http://127.0.0.1:18086/ >/dev/null 2>&1; then ready=true; break; fi
    if [ "$(docker inspect --format '{{.State.Running}}' "$server")" != true ]; then break; fi
    sleep 2
done
if [ "$ready" != true ]; then echo 'Disposable Umami did not become ready.' >&2; exit 1; fi

echo "Testing disposable Umami 3.4.0 with PostgreSQL 16."
UMAMI_TEST_URL=http://127.0.0.1:18086/ dotnet test "$repo_root/Umami.Net.Test/Umami.Net.Test.csproj" \
    --filter FullyQualifiedName~Umami34IntegrationTests \
    --results-directory "$repo_root/Umami.Net.Test/TestResults/integration" \
    --logger 'trx;LogFilePrefix=umami-integration' "$@"
