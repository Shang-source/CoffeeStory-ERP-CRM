# StoryCoffee Production Readiness Checklist

Use this before routing real customers to the system.

## Required Before First Customer

- [ ] Domain purchased and controlled through Cloudflare DNS.
- [ ] `APP_DOMAIN` points to the VPS public IP.
- [ ] HTTPS works with a valid certificate.
- [ ] `.env.production` exists only on the VPS and contains real secrets.
- [ ] `ASPNETCORE_ENVIRONMENT=Production`.
- [ ] `SeedData__Enabled=false`.
- [ ] PostgreSQL and Redis are private Docker services with no public ports.
- [ ] Admin password is changed from any demo/default value.
- [ ] Customer invite flow tested with a real test customer email.
- [ ] Invoice email tested with PDF attachment.
- [ ] Statement email tested with PDF attachment.
- [ ] Invoice PDF download tested from admin and customer portal.
- [ ] Customer portal login tested.
- [ ] `/health` returns healthy.
- [ ] `/ready` returns ready.

## Data Safety

- [ ] `scripts/vps-backup.sh` creates a PostgreSQL backup.
- [ ] document storage backup is included or S3/R2 storage is enabled.
- [ ] at least one backup is copied off-server.
- [ ] restore steps have been dry-read and assigned to one person.
- [ ] backup cron is installed.
- [ ] backup retention is configured.
- [ ] a pre-deployment backup is taken before every migration deployment.

## Security

- [ ] SSH uses key login.
- [ ] root/password SSH login is disabled where possible.
- [ ] firewall allows only `22`, `80`, and `443`.
- [ ] `.env.production` file permissions are restricted.
- [ ] JWT secret is 32+ random characters.
- [ ] document signing secret is 32+ random characters.
- [ ] SMTP/API credentials are not committed.
- [ ] Cloudflare account has 2FA enabled.
- [ ] GitHub account has 2FA enabled.

## Email Domain

- [ ] sender domain is verified in the email provider.
- [ ] SPF record is configured.
- [ ] DKIM record is configured.
- [ ] DMARC record exists.
- [ ] test invite email lands outside spam.
- [ ] test invoice email lands outside spam.

## Business Smoke Test

- [ ] Admin login.
- [ ] Create or open a customer.
- [ ] Send customer invite.
- [ ] Customer login.
- [ ] Customer account status displays correctly.
- [ ] Standing order exists or can be created.
- [ ] Generate order.
- [ ] Send order to production.
- [ ] Mark ready to ship.
- [ ] Ship and send invoice.
- [ ] Customer sees unpaid invoice.
- [ ] Record payment as admin.
- [ ] Customer sees invoice paid.
- [ ] Generate statement for outstanding invoices.
- [ ] Customer sees statement.

## Monitoring Minimum

- [ ] VPS disk usage checked.
- [ ] Docker container restart policy is enabled.
- [ ] API logs can be read with `docker compose logs api`.
- [ ] backup logs are written to `/opt/storycoffee/logs/backup.log`.
- [ ] there is a written contact path for customer-reported issues.

## Go / No-Go

Do not go live if any of these are true:

- `/ready` fails.
- backups have never succeeded.
- email sending is not verified.
- customer login is not verified.
- invoice PDF download fails.
- database is publicly exposed.
- `.env.production` is committed or copied into a public place.
