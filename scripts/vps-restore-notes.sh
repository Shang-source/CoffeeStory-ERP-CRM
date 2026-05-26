#!/usr/bin/env bash
set -euo pipefail

cat <<'EOF'
StoryCoffee restore is intentionally not automated by default.

Manual restore steps:
1. Confirm the target VPS is the correct environment.
2. Stop write traffic:
   docker compose --env-file .env.production -f infra/docker-compose.vps.yml stop api frontend caddy
3. Restore PostgreSQL from a selected backup:
   gunzip -c backups/YYYYMMDD-HHMMSS/storycoffee-postgres.sql.gz | \
     docker compose --env-file .env.production -f infra/docker-compose.vps.yml exec -T postgres \
     psql -U "$POSTGRES_USER" -d "$POSTGRES_DB"
4. Restore local documents if DocumentStorage__Provider=Local:
   cat backups/YYYYMMDD-HHMMSS/storycoffee-documents.tar.gz | \
     docker compose --env-file .env.production -f infra/docker-compose.vps.yml exec -T api \
     tar -C /var/storycoffee/documents -xzf -
5. Start services:
   docker compose --env-file .env.production -f infra/docker-compose.vps.yml up -d
6. Validate:
   curl -H "Host: $APP_DOMAIN" http://127.0.0.1/ready

Do not run restore commands during customer traffic without a written rollback decision.
EOF
