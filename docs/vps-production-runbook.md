# StoryCoffee VPS Production Runbook

This is the low-cost production path for a single 4GB VPS. It is designed for the expected scale of up to a few hundred B2B customers, not high-concurrency public traffic.

## Target Architecture

```text
Cloudflare domain / DNS
        |
        v
4GB Ubuntu VPS
        |
        v
Caddy HTTPS reverse proxy
        |
        +--> frontend nginx container
        +--> StoryCoffee .NET API container
        +--> PostgreSQL container with durable volume
        +--> Redis container
        +--> local document volume, backed up off-server
```

## VPS Baseline

- Ubuntu 24.04 LTS.
- 2 vCPU.
- 4GB RAM.
- 40GB+ SSD.
- Public IPv4.
- SSH key login enabled.
- Firewall allows only:
  - `22/tcp` SSH.
  - `80/tcp` HTTP.
  - `443/tcp` HTTPS.
- PostgreSQL and Redis are not exposed to the public internet.

## First-Time Server Setup

Install Docker Engine and the Compose plugin using the provider's recommended Ubuntu instructions. After Docker works:

```bash
sudo mkdir -p /opt/storycoffee
sudo chown "$USER":"$USER" /opt/storycoffee
cd /opt/storycoffee
git clone https://github.com/Shang-source/CoffeeStory-ERP-CRM.git .
cp .env.production.example .env.production
```

Edit `.env.production` and replace every `replace-with-*` value. Minimum required values:

- `APP_DOMAIN`
- `ACME_EMAIL`
- `POSTGRES_PASSWORD`
- `JWT_SECRET`
- `DOCUMENT_STORAGE_SIGNING_SECRET`
- `EMAIL_FROM_ADDRESS`
- SMTP values if `EMAIL_PROVIDER=Smtp`

Use these commands locally or on the server to generate strong secrets:

```bash
openssl rand -base64 48
```

## DNS

In Cloudflare DNS, create:

```text
Type: A
Name: app
Value: <VPS public IPv4>
Proxy: DNS only during first setup, proxied can be enabled later
```

Wait until the VPS resolves:

```bash
dig +short app.storycoffee.co.nz
```

## Deploy

From `/opt/storycoffee`:

```bash
scripts/vps-deploy.sh
```

The script builds containers, starts the stack, and checks `/ready` through Caddy using the configured host.

Manual status checks:

```bash
docker compose --env-file .env.production -f infra/docker-compose.vps.yml ps
docker compose --env-file .env.production -f infra/docker-compose.vps.yml logs --tail=100 api
curl -H "Host: $APP_DOMAIN" http://127.0.0.1/ready
```

Public checks after DNS and TLS are ready:

```bash
curl https://app.storycoffee.co.nz/ready
```

## Update Deployment

```bash
cd /opt/storycoffee
git pull --ff-only
scripts/vps-backup.sh
scripts/vps-deploy.sh
```

Always run a backup before deploying code that may include migrations.

## Rollback

If the new deployment fails before customer traffic:

```bash
git log --oneline -5
git checkout <previous-good-commit>
scripts/vps-deploy.sh
```

If a migration has already changed production data, do not blindly rollback code. Restore from a verified backup or write a forward fix.

## Backups

Run a manual backup:

```bash
scripts/vps-backup.sh
```

Schedule daily backups with cron:

```bash
crontab -e
```

Add:

```text
15 2 * * * cd /opt/storycoffee && scripts/vps-backup.sh >> logs/backup.log 2>&1
```

Create the log folder:

```bash
mkdir -p /opt/storycoffee/logs
```

If `BACKUP_S3_URI` is set in `.env.production`, backups are also uploaded off-server through AWS CLI. For Cloudflare R2, set:

- `BACKUP_S3_URI`
- `BACKUP_S3_ENDPOINT_URL`
- `AWS_ACCESS_KEY_ID`
- `AWS_SECRET_ACCESS_KEY`

## Restore

Restore is intentionally manual to avoid accidental production data loss:

```bash
scripts/vps-restore-notes.sh
```

Before restoring, record:

- selected backup timestamp
- current git commit
- reason for restore
- expected data loss window

## Email

For the lowest-cost first deployment, use SMTP through Brevo:

```text
EMAIL_PROVIDER=Smtp
EMAIL_SMTP_HOST=smtp-relay.brevo.com
EMAIL_SMTP_PORT=587
EMAIL_USE_START_TLS=true
```

The sending domain must have SPF/DKIM/DMARC records configured before inviting real customers.

## Document Storage

The lowest-cost default is:

```text
DOCUMENT_STORAGE_PROVIDER=Local
```

PDFs are stored on the VPS document volume and included in `scripts/vps-backup.sh`. If you want off-server document storage later, switch to `S3` with Cloudflare R2 or AWS S3 and set the S3-compatible values in `.env.production`.

## Production Rules

- Do not enable demo seed data.
- Do not expose PostgreSQL or Redis ports publicly.
- Do not commit `.env.production`.
- Do not store the only copy of backups on the VPS.
- Do not deploy migrations without a fresh backup.
- Do not use the local MailHog/LocalStack compose file for production.
