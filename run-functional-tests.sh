#!/usr/bin/env bash
# Run functional tests with Podman as the container runtime via Docker-compatible socket.
# The podman.socket systemd user unit must be enabled (it is by default on Fedora).
# Usage: ./run-functional-tests.sh [extra dotnet test args...]

set -euo pipefail

PODMAN_SOCK="/run/user/$(id -u)/podman/podman.sock"

# Ensure the podman socket unit is active (idempotent).
systemctl --user start podman.socket

# Trigger socket activation by opening a connection; the service needs a moment to start.
if ! curl --silent --max-time 2 --unix-socket "$PODMAN_SOCK" http://d/_ping &>/dev/null; then
    echo "Waking up podman API service..."
    # A first connection attempt triggers activation; retry after the service starts.
    curl --silent --max-time 1 --unix-socket "$PODMAN_SOCK" http://d/_ping &>/dev/null || true
    sleep 3
fi

if ! curl --silent --max-time 3 --unix-socket "$PODMAN_SOCK" http://d/_ping &>/dev/null; then
    echo "ERROR: Podman socket at $PODMAN_SOCK is not reachable." >&2
    exit 1
fi

echo "Podman socket ready at $PODMAN_SOCK"

export DOCKER_HOST="unix://${PODMAN_SOCK}"
export DOTNET_ASPIRE_CONTAINER_RUNTIME="podman"

exec dotnet test \
    tests/Application.FunctionalTests \
    --logger "console;verbosity=normal" \
    "$@"
