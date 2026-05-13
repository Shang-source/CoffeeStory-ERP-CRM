# StoryCoffee Backup and Restore

The durable data sources are PostgreSQL and document storage. Redis is treated as disposable infrastructure unless a future feature explicitly stores durable state there. The outbox is stored in PostgreSQL, not Redis.

## Local Docker Compose

Create a backup folder:

```bash
mkdir -p backups
```

Back up PostgreSQL:

```bash
docker compose -f infra/docker-compose.yml exec -T postgres \
  pg_dump -U storycoffee -d storycoffee --clean --if-exists \
  > backups/storycoffee-$(date +%Y%m%d-%H%M%S).sql
```

Restore PostgreSQL:

```bash
cat backups/storycoffee-YYYYMMDD-HHMMSS.sql | \
  docker compose -f infra/docker-compose.yml exec -T postgres \
  psql -U storycoffee -d storycoffee
```

List local volumes before destructive cleanup:

```bash
docker volume ls | grep storycoffee
docker compose -f infra/docker-compose.yml ps -a
docker compose -f infra/docker-compose.yml config > backups/docker-compose-config-$(date +%Y%m%d-%H%M%S).yaml
```

If you need to preserve LocalStack S3 objects in development, export them through AWS CLI pointed at LocalStack before removing volumes:

```bash
AWS_ACCESS_KEY_ID=test AWS_SECRET_ACCESS_KEY=test \
aws --endpoint-url=http://localhost:4566 s3 sync \
  s3://storycoffee-documents backups/storycoffee-documents
```

Restore LocalStack objects:

```bash
AWS_ACCESS_KEY_ID=test AWS_SECRET_ACCESS_KEY=test \
aws --endpoint-url=http://localhost:4566 s3 mb s3://storycoffee-documents

AWS_ACCESS_KEY_ID=test AWS_SECRET_ACCESS_KEY=test \
aws --endpoint-url=http://localhost:4566 s3 sync \
  backups/storycoffee-documents s3://storycoffee-documents
```

## Local Kubernetes

Port-forward PostgreSQL when the database is running in the local chart:

```bash
kubectl -n storycoffee port-forward service/storycoffee-postgres 15432:5432
```

Back up:

```bash
PGPASSWORD=storycoffee_password pg_dump \
  -h localhost -p 15432 -U storycoffee -d storycoffee \
  --clean --if-exists \
  > backups/storycoffee-k8s-$(date +%Y%m%d-%H%M%S).sql
```

Restore:

```bash
PGPASSWORD=storycoffee_password psql \
  -h localhost -p 15432 -U storycoffee -d storycoffee \
  < backups/storycoffee-k8s-YYYYMMDD-HHMMSS.sql
```

## AWS Production

### PostgreSQL

- Enable RDS automated backups and point-in-time recovery.
- Take a manual RDS snapshot before each production deployment that includes migrations.
- Test restore into a separate staging database at least once per release cycle.
- Keep production deletion protection enabled.
- Store backup metadata with deployment notes:
  - Snapshot ID.
  - Git commit SHA.
  - Migration version.
  - Deployment time.

### S3 Documents

- Enable bucket versioning in production.
- Enable server-side encryption.
- Keep block public access enabled.
- Restore individual documents by object version where possible.
- For bulk restore, sync from a known backup bucket or recovered versioned prefix.

### Redis

- Redis can be rebuilt from empty state for the current architecture.
- Do not treat Redis as the system of record.
- If future durable Redis usage is introduced, add ElastiCache snapshots and restore tests.

## Restore Validation

After any restore:

- Run API `/ready`.
- Log in as admin.
- Open orders, invoices, statements, and documents.
- Generate a test invoice PDF.
- Send a test email in staging or through a safe provider.
- Check outbox worker logs for stuck or duplicate processing.

## Minimum Retention Policy

- Local development: best-effort manual backups before destructive runtime changes.
- Staging: 7 days of RDS automated backups.
- Production: at least 35 days of RDS automated backups plus pre-release snapshots.
- S3 production documents: versioning enabled, lifecycle deletion only after a written retention decision.
