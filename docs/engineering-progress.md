# StoryCoffee Engineering Progress

## Implemented

- Redis package and typed options are registered, with `/ready` checking Redis when enabled.
- Quartz hosting package is registered and can run the standing-order generation job when `Quartz:Enabled=true`.
- Serilog is wired as the host logger with console output and configurable minimum levels.
- FluentValidation is wired through an MVC action filter for key write contracts.
- Docker Compose includes PostgreSQL, Redis, API, frontend, LocalStack, and MailHog.
- Helm includes Redis and routes `/api`, `/health`, and `/ready` to the API.
- CI, Playwright workflow coverage, k6 smoke, and Terraform placeholders are present.
- OrderWorkflow, Production, and Catalog use cases now orchestrate repositories instead of direct EF service classes.
- `IClock`, `IUnitOfWork`, `outbox_messages`, and an Outbox retry worker are present for transactional side-effect handling.
- SMTP, S3-compatible storage, and QuestPDF provider implementations are wired behind options.
- API integration tests use PostgreSQL/Redis Testcontainers; unit-style service tests still use EF InMemory for speed.
- API integration tests reset PostgreSQL state per test to avoid cross-test order/customer/production pollution.
- The frontend imports generated OpenAPI operation types from `frontend/src/shared/api/generated/schema.ts` through `frontend/src/shared/api/openapi.ts`.
- CI checks OpenAPI generated client drift, and `scripts/smoke-storycoffee.mjs` verifies the main seeded API workflow.
- The legacy `production_progress` model is removed from the current schema and dropped by migration.
- Customer-specific price books now drive standing-order item pricing and generated order snapshots.
- Suspended or archived customer accounts are blocked from login and authenticated customer APIs.
- Admin order batch-to-production uses the batch API from the frontend.
- AWS SES v2 is available as a production email provider behind the existing `IEmailSender` interface.
- Outbox processing now claims PostgreSQL rows with `FOR UPDATE SKIP LOCKED` and reclaims stale processing locks.
- SES webhook intake records delivery events and reconciles Bounce/Complaint/Delivery state back to EmailLog, invoice email status, and statement email status.
- SES/SNS webhook handling verifies AWS SNS signatures by default, validates the signing certificate URL/topic ARN when configured, and can auto-confirm subscriptions after verification.
- Backend namespaces now align with project boundaries: `StoryCoffee.Domain`, `StoryCoffee.Contracts`, `StoryCoffee.Application.*`, `StoryCoffee.Infrastructure.*`, and `StoryCoffee.Api.*`.
- Dependency injection is split by layer: API registers HTTP concerns, Application registers use cases, and Infrastructure registers persistence/providers/jobs/workers.
- Authentication and infrastructure-facing service ports are exposed from Application interfaces instead of leaking infrastructure implementation namespaces into controllers.
- Application, Infrastructure, API controllers, and tests are grouped by bounded module folders so business areas are easier to navigate and evolve independently.
- Full backend tests pass under OrbStack/Testcontainers, and an isolated fresh `docker compose up --build` validates API/frontend/PostgreSQL/Redis/LocalStack/MailHog health without touching the existing dev stack.
- The previous coupled `CatalogUseCase` is split into `CustomerUseCase` and `ProductCatalogUseCase`, with separate repository ports, infrastructure repositories, DI registrations, controllers, and module tests.
- The local project folder is initialized as a Git repository, connected to GitHub, and pushed to `origin/main`.
- Frontend P0 Vitest coverage now includes login success/failure, role guard redirects, admin batch-to-production, admin price-book save/repricing, and customer standing-order effective price rendering.
- GitHub Actions CI is green on `main` for backend, frontend, API contract, and Helm checks.
- Helm now has separate dev/staging/prod values, optional in-chart PostgreSQL/Redis deployment, external secret support, ALB-ready ingress annotations, and AWS-ready S3/SES/RDS/Redis placeholders.
- AWS production setup and backup/restore procedures are documented without requiring real AWS resources in the current development environment.
- Customer portal P0 now has invoice and statement detail APIs/pages, TanStack Query-backed customer dashboard/invoice/statement reads, and backend/frontend tests for customer detail authorization and rendering.
- Frontend P1 contract cleanup now separates authenticated API blob downloads from external presigned downloads and uses OpenAPI-derived query contracts for audit/email CSV exports.
- k6 now exercises platform readiness plus Admin and Customer read APIs instead of only `/health`.
- Playwright smoke coverage now includes Customer dashboard, invoice detail, statement detail, and account settings after ensuring seeded financial data exists.
- CI now validates Helm lint, Docker Compose config for local/test stacks, and backend/frontend Docker image builds.
- Admin Invoices, Payments, and Statements now use TanStack Query for server state instead of page-local duplicated loading/error/refetch state.
- E2E runs can opt into a protected Development/Testing-only reset endpoint through `infra/docker-compose.e2e.yml` and `pnpm test:e2e:reset`.

## Remaining

- Replace AWS placeholder values with real ECR images, RDS, ElastiCache, S3, SES/SNS, IAM/IRSA, and External Secrets configuration before production deployment.
- Add workload-specific k6 scenarios and thresholds once realistic traffic targets are defined.
