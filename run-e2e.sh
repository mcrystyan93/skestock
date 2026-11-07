#!/usr/bin/env bash
# Bring up an ephemeral, seeded stack (no data volumes), run Playwright against it, tear it down.
# Usage: ./run-e2e.sh [--keep] [-- <playwright args>]
# Credentials are generated per run, passed only through the environment, never printed or written.
# Playwright args are relative to src/Client. Random credentials are visible in `podman inspect` while the stack runs.
# The e2e stack uses the dev ports, so it cannot run next to the dev stack.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")"

KEEP=0
if [[ "${1:-}" == "--keep" ]]; then KEEP=1; shift; fi
if [[ "${1:-}" == "--" ]]; then shift; fi

APPHOST="$PWD/src/AppHost/AppHost.csproj"

PODMAN_SOCK="/run/user/$(id -u)/podman/podman.sock"
systemctl --user start podman.socket
if ! curl --silent --max-time 2 --unix-socket "$PODMAN_SOCK" http://d/_ping &>/dev/null; then
    echo "Waking up podman API service..."
    curl --silent --max-time 1 --unix-socket "$PODMAN_SOCK" http://d/_ping &>/dev/null || true
    sleep 3
fi
if ! curl --silent --max-time 3 --unix-socket "$PODMAN_SOCK" http://d/_ping &>/dev/null; then
    echo "ERROR: Podman socket at $PODMAN_SOCK is not reachable." >&2
    exit 1
fi
export DOCKER_HOST="unix://${PODMAN_SOCK}"
export DOTNET_ASPIRE_CONTAINER_RUNTIME="podman"

for port in 7001 1433 6379 8081 10000 10001 10002; do
    if [[ -n "$(ss -ltnH "sport = :$port")" ]]; then
        echo "ERROR: port $port is busy (dev stack running?). Stop it first (aspire stop); e2e and dev share fixed ports." >&2
        exit 1
    fi
done

if [[ ! -d src/Client/node_modules ]]; then
    echo "ERROR: src/Client/node_modules missing. Run: cd src/Client && npm ci" >&2
    exit 1
fi

# Hex plus a fixed suffix satisfies the SQL Server and Identity password policies and is safe in connection strings.
gen() { echo "$(openssl rand -hex 16)Aa1_"; }
ADMIN_EMAIL="e2e-admin@skestock.test"
ADMIN_PASSWORD="$(gen)"

# Env vars carry hyphens, which `export` rejects, so they are passed through `env`.
# This overrides user-secrets, so dev secrets are neither read nor changed.
APPHOST_ENV=(
    "E2E=true"
    "Parameters__SeedAdministratorEmail=$ADMIN_EMAIL"
    "Parameters__SeedAdministratorPassword=$ADMIN_PASSWORD"
    "Parameters__sql-password=$(gen)"
    "Parameters__redis-password=$(gen)"
    "Parameters__OpenApiSettingsApiKey=e2e-dummy-key"
)

STARTED=0
cleanup() {
    if [[ $STARTED -eq 1 && $KEEP -eq 0 ]]; then
        echo "Stopping e2e stack..."
        aspire stop --apphost "$APPHOST" --non-interactive || true
    elif [[ $STARTED -eq 1 ]]; then
        echo "Stack left running (--keep). Stop it with: aspire stop --apphost $APPHOST"
    fi
}
trap cleanup EXIT

STARTED=1
env "${APPHOST_ENV[@]}" aspire start --apphost "$APPHOST" --non-interactive

wait_for() {
    if ! aspire wait "$1" --apphost "$APPHOST" --status healthy --timeout "$2" --non-interactive; then
        echo "ERROR: resource '$1' did not become healthy within ${2}s." >&2
        exit 1
    fi
}
wait_for webapi 300
wait_for webfrontend 120

cd src/Client
PLAYWRIGHT_BASE_URL="http://127.0.0.1:7001" \
PLAYWRIGHT_LOGIN_EMAIL="$ADMIN_EMAIL" \
PLAYWRIGHT_LOGIN_PASSWORD="$ADMIN_PASSWORD" \
PLAYWRIGHT_REQUIRE_LOGIN=1 \
    npx playwright test "$@"
