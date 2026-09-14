# Fedora deployment

The production workflow builds the Web and Worker images on GitHub-hosted runners, pushes immutable commit-tagged images to GHCR, and deploys them to Fedora through Cloudflare Access SSH. The mini PC runs the complete stack with rootless Podman and systemd Quadlet.

## GitHub production Environment

Configure a GitHub Environment named `production`. Keep non-sensitive values in Environment variables and credentials in Environment secrets.

Environment variables:

| Name | Purpose |
| --- | --- |
| `DEPLOY_HOST` | Cloudflare SSH application hostname used by SSH, such as `ssh.example.com` |
| `DEPLOY_USER` | Dedicated rootless Podman user |
| `PUBLIC_ORIGINS` | Comma-separated browser origins allowed by Web |
| `STORAGE_PUBLIC_BLOB_ENDPOINT` | Absolute HTTPS endpoint used in browser-facing Azurite SAS URLs, such as `https://blob.example.com` |
| `OPENAI_MODEL` | OpenAI model name |
| `DEPLOY_HEALTH_URL` | Public URL checked after deployment, normally `http://host:7001/scalar` |

Environment secrets:

| Name | Purpose |
| --- | --- |
| `SQL_PASSWORD` | SQL Server SA password |
| `REDIS_PASSWORD` | Redis password |
| `OPENAI_API_KEY` | OpenAI document extraction key |
| `DEPLOY_SSH_PRIVATE_KEY` | SSH private key for the deploy user |
| `DEPLOY_KNOWN_HOSTS` | Pinned SSH host key line(s) |
| `CLOUDFLARE_ACCESS_CLIENT_ID` | Cloudflare Access service-token client ID |
| `CLOUDFLARE_ACCESS_CLIENT_SECRET` | Cloudflare Access service-token client secret |
| `GHCR_DEPLOY_USERNAME` | GHCR account with read access to the packages |
| `GHCR_DEPLOY_TOKEN` | GHCR token with `read:packages` |

The workflow creates the runtime environment file from this contract, transfers it atomically, and installs it with mode `0600`. Do not commit the generated file.

## Fedora prerequisites

Install Podman and the Quadlet package, create a dedicated deploy user, and enable user services to run without an interactive login:

```bash
sudo dnf install podman
loginctl enable-linger <deploy-user>
systemctl --user daemon-reload
```

The deploy user must be able to run `podman`, `systemctl --user`, and access the configured SSH key. Open only the Web port (`7001`) and the Azurite blob/queue ports needed by browser-direct uploads. Keep SQL Server, Redis, and table administration ports restricted to localhost or the trusted LAN.

## Cloudflare Access SSH

Create a Cloudflare Tunnel published application for SSH:

```text
ssh.example.com -> ssh://localhost:22
```

Protect the application with an Access policy that allows the service token used by the GitHub Environment. Store the generated client ID and secret as `CLOUDFLARE_ACCESS_CLIENT_ID` and `CLOUDFLARE_ACCESS_CLIENT_SECRET` secrets. Set `DEPLOY_HOST` to the published SSH hostname, not the tunnel ID or the mini PC's private address.

`DEPLOY_KNOWN_HOSTS` must contain the Fedora SSH host key for that hostname. Capture and pin it through a trusted path; it is the Fedora SSH key, not a Cloudflare certificate.

Back up the Podman volumes `skestock-db-data`, `skestock-cache-data`, and `storage-data` before treating the mini PC as production infrastructure. Azurite is still an emulator; migrate Blob/Queue to Azure Storage before requiring cloud-grade durability.

## Rollback

The previous Web and Worker image tags are retained locally as `localhost/skestock-web:previous` and `localhost/skestock-worker:previous`. A rollback should retag the desired GHCR SHA as `current`, restart the two application units, and repeat the health check. Infrastructure services are not recreated during normal application releases.
