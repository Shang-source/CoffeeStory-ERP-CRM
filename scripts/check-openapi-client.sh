#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

"$ROOT_DIR/scripts/generate-openapi-client.sh"

cd "$ROOT_DIR"
if ! git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  echo "No git repository detected; generated OpenAPI client without diff validation."
  exit 0
fi

if ! git diff --exit-code -- frontend/src/shared/api/generated/schema.ts; then
  echo "OpenAPI generated client is out of date. Run pnpm generate:api and commit the updated schema." >&2
  exit 1
fi
