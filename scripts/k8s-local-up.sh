#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
NAMESPACE="${NAMESPACE:-storycoffee}"

cd "$ROOT_DIR"

command -v docker >/dev/null || { echo "docker is required"; exit 1; }
command -v helm >/dev/null || { echo "helm is required"; exit 1; }
command -v kubectl >/dev/null || { echo "kubectl is required"; exit 1; }

docker build -t storycoffee-api:dev -f backend/src/StoryCoffee.Api/Dockerfile .
docker build -t storycoffee-frontend:dev -f frontend/Dockerfile .

if [[ "${SKIP_K8S_IMAGE_IMPORT:-false}" != "true" ]]; then
  image_archive="$(mktemp -t storycoffee-k8s-images.XXXXXX.tar)"
  importer_pod="storycoffee-image-importer"

  cleanup_importer() {
    kubectl -n "$NAMESPACE" delete pod "$importer_pod" --ignore-not-found >/dev/null 2>&1 || true
    rm -f "$image_archive"
  }
  trap cleanup_importer EXIT

  docker save storycoffee-api:dev storycoffee-frontend:dev -o "$image_archive"

  kubectl create namespace "$NAMESPACE" --dry-run=client -o yaml | kubectl apply -f -
  kubectl -n "$NAMESPACE" delete pod "$importer_pod" --ignore-not-found >/dev/null 2>&1 || true

  cat <<EOF | kubectl apply -f -
apiVersion: v1
kind: Pod
metadata:
  name: $importer_pod
  namespace: $NAMESPACE
spec:
  restartPolicy: Never
  hostPID: true
  containers:
    - name: importer
      image: storycoffee-api:dev
      imagePullPolicy: IfNotPresent
      securityContext:
        privileged: true
      command: ["/bin/sh", "-c", "sleep 3600"]
      volumeMounts:
        - name: host-root
          mountPath: /host
        - name: host-tmp
          mountPath: /node-tmp
  volumes:
    - name: host-root
      hostPath:
        path: /
        type: Directory
    - name: host-tmp
      hostPath:
        path: /tmp
        type: Directory
EOF

  kubectl -n "$NAMESPACE" wait --for=condition=Ready "pod/$importer_pod" --timeout=120s
  kubectl -n "$NAMESPACE" cp "$image_archive" "$importer_pod:/node-tmp/storycoffee-k8s-images.tar"
  kubectl -n "$NAMESPACE" exec "$importer_pod" -- chroot /host /bin/sh -c '
    ctr -n k8s.io images rm docker.io/library/storycoffee-api:dev docker.io/library/storycoffee-frontend:dev >/dev/null 2>&1 || true
    ctr -n k8s.io images import /tmp/storycoffee-k8s-images.tar
    rm -f /tmp/storycoffee-k8s-images.tar
  '
fi

helm upgrade --install storycoffee infra/helm/storycoffee \
  --namespace "$NAMESPACE" \
  --create-namespace \
  -f infra/helm/storycoffee/values-local.yaml

kubectl -n "$NAMESPACE" rollout restart deployment/storycoffee-api deployment/storycoffee-frontend

kubectl -n "$NAMESPACE" rollout status deployment/storycoffee-postgres --timeout=600s
kubectl -n "$NAMESPACE" rollout status deployment/storycoffee-redis --timeout=600s
kubectl -n "$NAMESPACE" rollout status deployment/storycoffee-localstack --timeout=600s
kubectl -n "$NAMESPACE" rollout status deployment/storycoffee-mailhog --timeout=600s
kubectl -n "$NAMESPACE" rollout status deployment/storycoffee-api --timeout=600s
kubectl -n "$NAMESPACE" rollout status deployment/storycoffee-frontend --timeout=600s

kubectl -n "$NAMESPACE" get pods
