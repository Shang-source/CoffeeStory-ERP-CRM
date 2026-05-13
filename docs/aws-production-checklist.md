# StoryCoffee AWS Production Checklist

This checklist keeps the current codebase deployable without requiring real AWS resources today. Local development continues to use Docker Compose, LocalStack, and MailHog; staging and production Helm values are AWS-ready placeholders that should be filled when the AWS account is available.

## Environment Values

- Development: `infra/helm/storycoffee/values-dev.yaml`
  - In-cluster PostgreSQL and Redis.
  - LocalStack for S3-compatible document storage.
  - MailHog SMTP for email capture.
  - Demo seed data is allowed through development-only seed options.
- Staging: `infra/helm/storycoffee/values-staging.yaml`
  - External RDS PostgreSQL, ElastiCache Redis, S3, SES, SNS, and an existing Kubernetes secret.
  - No in-cluster PostgreSQL, LocalStack, or MailHog.
- Production: `infra/helm/storycoffee/values-prod.yaml`
  - Same external dependency shape as staging.
  - Two API/frontend replicas by default.
  - No demo seed data.

## Required AWS Resources

### Network and Kubernetes

- Choose the primary region, currently assumed to be `ap-southeast-2`.
- Create or select an EKS cluster with private worker nodes.
- Install the AWS Load Balancer Controller if using ALB ingress.
- Configure Route 53 hosted zones for `storycoffee.co.nz`.
- Create ACM certificates for `staging.storycoffee.co.nz` and `app.storycoffee.co.nz`.
- Wire ALB ingress DNS records through Route 53 aliases.

### Images

- Create ECR repositories:
  - `storycoffee-api`
  - `storycoffee-frontend`
- Push immutable image tags for each deployment.
- Replace the placeholder image values in the staging/prod Helm files:
  - `<aws-account-id>.dkr.ecr.ap-southeast-2.amazonaws.com/storycoffee-api:<tag>`
  - `<aws-account-id>.dkr.ecr.ap-southeast-2.amazonaws.com/storycoffee-frontend:<tag>`

### Database

- Create RDS PostgreSQL 16 or later.
- Enable automated backups and point-in-time recovery.
- Enable deletion protection for production.
- Restrict security groups to the EKS worker-node security group.
- Store the API connection string in the environment secret as `ConnectionStrings__DefaultConnection`.
- Run migrations through the API startup migration path or a controlled one-off job before traffic cutover.

### Redis

- Create ElastiCache Redis if Redis health checks, locks, or cache features remain enabled.
- Store the endpoint in Helm as `api.redisConnectionString`.
- Set `redis.enabled=true` and `redis.deploy=false` for external Redis.
- If Redis is intentionally not used, set `redis.enabled=false` and remove the external endpoint requirement.

### Document Storage

- Create S3 buckets:
  - `storycoffee-staging-documents`
  - `storycoffee-prod-documents`
- Enable block public access.
- Enable server-side encryption.
- Enable versioning for production.
- Add lifecycle rules for old generated documents if retention policy allows it.
- Prefer IRSA for S3 access instead of static AWS keys.
- Keep `DocumentStorage__Provider=S3` and `DocumentStorage__ForcePathStyle=false` in staging/prod.

### Email

- Verify the SES sending domain.
- Configure DKIM and a custom MAIL FROM domain.
- Request production SES sending access before real customers are invited.
- Create SES configuration sets:
  - `storycoffee-staging`
  - `storycoffee-prod`
- Create SNS topics for SES delivery events:
  - `storycoffee-staging-ses-events`
  - `storycoffee-prod-ses-events`
- Subscribe the API webhook endpoint:
  - `https://staging.storycoffee.co.nz/api/webhooks/ses`
  - `https://app.storycoffee.co.nz/api/webhooks/ses`
- Keep `Email__VerifySnsSignature=true`.
- Set `Email__SnsTopicArn` to the exact topic ARN for each environment.
- Only enable `Email__AutoConfirmSnsSubscriptions=true` after public HTTPS ingress works.

### IAM

- Use IRSA for the API service account before production traffic.
- Minimum permissions:
  - S3 object read/write for the document bucket.
  - SES send email through SES v2.
  - SNS subscription confirmation only if auto-confirm is enabled.
- Avoid long-lived AWS access keys in Kubernetes secrets unless there is no IRSA option.

## Kubernetes Secret Contract

Staging and production Helm values set `secret.create=false`, so an existing secret must be created before deploying:

```bash
kubectl -n storycoffee create secret generic storycoffee-prod-secret \
  --from-literal=ConnectionStrings__DefaultConnection='Host=...;Port=5432;Database=storycoffee;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true' \
  --from-literal=Jwt__Secret='<strong-random-secret>' \
  --from-literal=DocumentStorage__SigningSecret='<strong-random-secret>' \
  --from-literal=Email__WebhookSecret='<strong-random-secret>'
```

Optional keys when static credentials are used:

```text
DocumentStorage__AccessKey
DocumentStorage__SecretKey
Email__SmtpUsername
Email__SmtpPassword
```

For production, prefer AWS Secrets Manager plus External Secrets Operator instead of manually creating native Kubernetes secrets.

## Deployment Commands

Render staging:

```bash
helm template storycoffee infra/helm/storycoffee \
  -f infra/helm/storycoffee/values-staging.yaml
```

Deploy staging:

```bash
helm upgrade --install storycoffee infra/helm/storycoffee \
  --namespace storycoffee-staging \
  --create-namespace \
  -f infra/helm/storycoffee/values-staging.yaml
```

Deploy production:

```bash
helm upgrade --install storycoffee infra/helm/storycoffee \
  --namespace storycoffee \
  --create-namespace \
  -f infra/helm/storycoffee/values-prod.yaml
```

## Production Readiness Gate

Do not route real customer traffic until all of these pass:

- `kubectl get pods -n storycoffee` shows all app pods ready.
- `GET /health` returns healthy.
- `GET /ready` validates PostgreSQL, Redis when enabled, and document storage.
- Login works for admin and customer users.
- Invoice PDF generation uploads to S3 and returns a presigned URL.
- SES sends a test invoice email.
- SES delivery/bounce/complaint SNS events reach `/api/webhooks/ses`.
- RDS backup and restore has been tested in a non-production database.
