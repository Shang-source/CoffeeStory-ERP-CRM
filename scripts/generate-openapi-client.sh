#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OPENAPI_URL="${STORYCOFFEE_OPENAPI_URL:-http://localhost:5080/swagger/v1/swagger.json}"
OPENAPI_FILE="${TMPDIR:-/tmp}/storycoffee-openapi.json"

curl -fsSL "$OPENAPI_URL" -o "$OPENAPI_FILE"

cd "$ROOT_DIR"
pnpm --filter frontend exec openapi-typescript "$OPENAPI_FILE" -o src/shared/api/generated/schema.ts
