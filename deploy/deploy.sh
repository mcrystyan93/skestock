#!/usr/bin/env bash
set -euo pipefail

config_dir="${XDG_CONFIG_HOME:-"$HOME/.config"}/skestock"
quadlet_source="$config_dir/quadlet"
quadlet_dir="${XDG_CONFIG_HOME:-"$HOME/.config"}/containers/systemd"
environment_file="$config_dir/production.env"
new_environment_file="$config_dir/production.env.new"

if [[ -s "$new_environment_file" ]]; then
  mv -f "$new_environment_file" "$environment_file"
fi

if [[ ! -s "$environment_file" ]]; then
  printf 'Missing runtime environment file: %s\n' "$environment_file" >&2
  exit 1
fi

get_environment_value() {
  local key="$1"
  awk -F= -v wanted="$key" '$1 == wanted { sub(/^[^=]*=/, ""); print; exit }' "$environment_file"
}

image_tag="$(get_environment_value SKESTOCK_IMAGE_TAG)"
web_image="$(get_environment_value SKESTOCK_WEB_IMAGE)"
worker_image="$(get_environment_value SKESTOCK_WORKER_IMAGE)"

if [[ -z "$image_tag" || -z "$web_image" || -z "$worker_image" ]]; then
  printf 'Runtime environment is missing release image settings.\n' >&2
  exit 1
fi

if ! [[ "$image_tag" =~ ^[a-zA-Z0-9][a-zA-Z0-9._-]*$ ]]; then
  printf 'Invalid image tag: %s\n' "$image_tag" >&2
  exit 1
fi

for unit_file in "$quadlet_source"/*; do
  [[ -f "$unit_file" ]] || continue
  install -m 0644 "$unit_file" "$quadlet_dir/$(basename "$unit_file")"
done

chmod 0600 "$environment_file"

podman pull "$web_image"
podman pull "$worker_image"

if podman image exists localhost/skestock-web:current; then
  podman tag localhost/skestock-web:current localhost/skestock-web:previous
fi
if podman image exists localhost/skestock-worker:current; then
  podman tag localhost/skestock-worker:current localhost/skestock-worker:previous
fi

podman tag "$web_image" localhost/skestock-web:current
podman tag "$worker_image" localhost/skestock-worker:current

systemctl --user daemon-reload
systemctl --user enable skestock-db.service skestock-cache.service skestock-storage.service
systemctl --user enable skestock-web.service skestock-worker.service
systemctl --user start skestock-db.service skestock-cache.service skestock-storage.service
systemctl --user restart skestock-web.service skestock-worker.service

systemctl --user is-active --quiet skestock-db.service
systemctl --user is-active --quiet skestock-cache.service
systemctl --user is-active --quiet skestock-storage.service
systemctl --user is-active --quiet skestock-web.service
systemctl --user is-active --quiet skestock-worker.service

printf 'Deployed %s\n' "$image_tag"
