#!/usr/bin/env bash
set -euo pipefail

NAMESPACE="${NAMESPACE:-storycoffee}"

cleanup() {
  jobs -p | xargs -r kill
}
trap cleanup EXIT

kubectl -n "$NAMESPACE" port-forward svc/storycoffee-frontend 8080:80 &
kubectl -n "$NAMESPACE" port-forward svc/storycoffee-api 5080:8080 &
kubectl -n "$NAMESPACE" port-forward svc/storycoffee-mailhog 8025:8025 &

echo "StoryCoffee frontend: http://localhost:8080"
echo "StoryCoffee API health: http://localhost:5080/health"
echo "MailHog: http://localhost:8025"
echo "Press Ctrl+C to stop port-forwarding."

wait
