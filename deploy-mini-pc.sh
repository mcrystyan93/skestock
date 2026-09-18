#!/usr/bin/env bash
set -euo pipefail

REMOTE='ssh://cristian@192.168.0.143:22/run/user/1000/podman/podman.sock'
COMPOSE='aspire-output/docker-compose.yaml'
BUILD_VERSION="${BUILD_VERSION:-$(date -u +%Y%m%d%H%M%S)}"

export CONTAINER_HOST="$REMOTE"

echo "Deploying Angular build: $BUILD_VERSION"

podman-compose \
  -p aspire-output \
  -f "$COMPOSE" \
  up -d \
  --build \
  --build-arg "ANGULAR_BUILD_VERSION=$BUILD_VERSION" \
  --force-recreate \
  --no-deps \
  webapi worker

podman --remote --url "$REMOTE" ps