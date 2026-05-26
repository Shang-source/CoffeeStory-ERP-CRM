#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${ENV_FILE:-$ROOT_DIR/.env.production}"
COMPOSE_FILE="$ROOT_DIR/infra/docker-compose.vps.yml"

if [ ! -f "$ENV_FILE" ]; then
  echo "Missing $ENV_FILE." >&2
  exit 1
fi

set -a
# shellcheck disable=SC1090
source "$ENV_FILE"
set +a

BACKUP_DIR="${BACKUP_DIR:-$ROOT_DIR/backups}"
RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-30}"
TIMESTAMP="$(date -u +%Y%m%d-%H%M%S)"
TARGET_DIR="$BACKUP_DIR/$TIMESTAMP"

mkdir -p "$TARGET_DIR"
cd "$ROOT_DIR"

echo "Backing up PostgreSQL..."
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" exec -T postgres \
  pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" --clean --if-exists \
  > "$TARGET_DIR/storycoffee-postgres.sql"

gzip "$TARGET_DIR/storycoffee-postgres.sql"

if [ "${DOCUMENT_STORAGE_PROVIDER:-Local}" = "Local" ]; then
  echo "Backing up local document storage volume..."
  docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" exec -T api \
    sh -c 'mkdir -p /var/storycoffee/documents && tar -C /var/storycoffee/documents -czf - .' \
    > "$TARGET_DIR/storycoffee-documents.tar.gz"
fi

docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" config > "$TARGET_DIR/docker-compose-rendered.yml"

if [ -n "${BACKUP_S3_URI:-}" ]; then
  if ! command -v aws >/dev/null 2>&1; then
    echo "BACKUP_S3_URI is set but aws CLI is not installed." >&2
    exit 1
  fi

  echo "Uploading backup to $BACKUP_S3_URI/$TIMESTAMP..."
  if [ -n "${BACKUP_S3_ENDPOINT_URL:-}" ]; then
    aws --endpoint-url "$BACKUP_S3_ENDPOINT_URL" s3 sync "$TARGET_DIR" "$BACKUP_S3_URI/$TIMESTAMP"
  else
    aws s3 sync "$TARGET_DIR" "$BACKUP_S3_URI/$TIMESTAMP"
  fi
fi

find "$BACKUP_DIR" -mindepth 1 -maxdepth 1 -type d -mtime +"$RETENTION_DAYS" -print -exec rm -rf {} +

echo "Backup complete: $TARGET_DIR"
