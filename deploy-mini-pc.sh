#!/usr/bin/env bash
set -euo pipefail

REMOTE='ssh://cristian@192.168.0.143:22/run/user/1000/podman/podman.sock'
COMPOSE='aspire-output/docker-compose.yaml'
SSH_KEY="${PODMAN_SSH_KEY:-$HOME/.ssh/id_ed25519}"
BUILD_VERSION="${BUILD_VERSION:-$(date -u +%Y%m%d%H%M%S)}"

if [[ ! -r "$SSH_KEY" ]]; then
  echo "SSH key not readable: $SSH_KEY" >&2
  exit 1
fi

export CONTAINER_HOST="$REMOTE"
export CONTAINER_SSHKEY="$SSH_KEY"

echo "Checking remote Podman..."
podman --remote --url "$REMOTE" --identity "$SSH_KEY" info >/dev/null

echo "Building Web and Worker images: $BUILD_VERSION"
podman-compose \
  -p aspire-output \
  -f "$COMPOSE" \
  build \
  --build-arg "ANGULAR_BUILD_VERSION=$BUILD_VERSION" \
  webapi worker

echo "Replacing Web and Worker containers..."
podman-compose \
  -p aspire-output \
  -f "$COMPOSE" \
  up -d \
  --no-build \
  --force-recreate \
  --no-deps \
  webapi worker

echo "Deployment status:"
podman --remote --url "$REMOTE" --identity "$SSH_KEY" ps
