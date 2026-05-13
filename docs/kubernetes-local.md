# StoryCoffee Local Kubernetes

This project uses Docker Desktop Kubernetes for local cluster testing.

## Start Runtime

```bash
open -a Docker
docker desktop status
docker desktop kubernetes status
kubectl config use-context docker-desktop
kubectl get nodes
```

If Kubernetes is disabled, enable it in Docker Desktop Settings, or use the Docker Desktop backend start endpoint:

```bash
curl --unix-socket "$HOME/Library/Containers/com.docker.docker/Data/backend.sock" \
  -X POST http://localhost/kubernetes/start
```

## Deploy

```bash
cd /Users/carashang/auckland/Project/coffee
scripts/k8s-local-up.sh
```

The script builds local `storycoffee-api:dev` and `storycoffee-frontend:dev` images, imports them into Docker Desktop Kubernetes containerd, applies the Helm chart, and restarts the API/frontend deployments so the local dev pods use the latest build.

The local Helm values deploy:

- API and frontend images built locally.
- PostgreSQL and Redis inside the cluster.
- LocalStack S3 for PDF storage.
- MailHog for SMTP capture.

## Access

```bash
scripts/k8s-local-port-forward.sh
```

Open:

- Frontend: `http://localhost:8080`
- API health: `http://localhost:5080/health`
- MailHog: `http://localhost:8025`

## Stop

```bash
scripts/k8s-local-down.sh
```

## Production Direction

For production Kubernetes, keep the app workloads in the cluster but use managed external dependencies:

- PostgreSQL: RDS, Cloud SQL, or Azure Database for PostgreSQL.
- Redis: managed Redis.
- Object storage: S3-compatible bucket.
- Email: AWS SES with SNS delivery events posted to `/api/webhooks/ses`.
- Webhooks: keep `Email__VerifySnsSignature=true`, set `Email__SnsTopicArn`, and only enable `Email__AutoConfirmSnsSubscriptions` after ingress/TLS is ready.
- Secrets: external secret manager or sealed secrets.
