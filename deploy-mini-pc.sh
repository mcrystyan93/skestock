#!/usr/bin/env bash
set -euo pipefail

REMOTE='ssh://cristian@192.168.0.143:22/run/user/1000/podman/podman.sock'
COMPOSE='aspire-output/docker-compose.yaml'

export CONTAINER_HOST="$REMOTE"

podman-compose \
  -p aspire-output \
  -f "$COMPOSE" \
  up -d \
  --build \
  --force-recreate \
  --no-deps \
  webapi worker

podman --remote --url "$REMOTE" ps