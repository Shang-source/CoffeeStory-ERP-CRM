#!/usr/bin/env bash
set -euo pipefail

NAMESPACE="${NAMESPACE:-storycoffee}"

helm uninstall storycoffee --namespace "$NAMESPACE" || true
kubectl delete namespace "$NAMESPACE" --ignore-not-found=true
