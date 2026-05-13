# StoryCoffee

StoryCoffee is a B2B order and invoice management system for recurring wholesale coffee customers.

## Workspace

- `frontend` - React, TypeScript, Vite, Material UI
- `backend` - ASP.NET Core Web API, EF Core, PostgreSQL
- `infra` - Docker Compose and Kubernetes/Helm deployment assets
- `docs` - product, backend, engineering, and changelog documents

## Backend Structure

- `backend/src/StoryCoffee.Api` - ASP.NET Core host, controllers, middleware, validation, and DI composition.
- `backend/src/StoryCoffee.Contracts` - public API request/response contracts and DTOs.
- `backend/src/StoryCoffee.Domain` - EF Core entity models, enums, and durable domain state.
- `backend/src/StoryCoffee.Application` - use cases, repository/service interfaces, mapping, errors, clock/unit-of-work/outbox abstractions.
- `backend/src/StoryCoffee.Infrastructure` - EF Core `AppDbContext`, migrations, repositories, auth, providers, jobs, options, and external integrations.
- `backend/tests/StoryCoffee.Tests` - backend unit, workflow, architecture, and integration tests.

Database tables and relationships are documented in `docs/database-schema.md`; backend dependency rules are documented in `docs/backend-architecture.md`.

## Demo Accounts

- Admin: `admin@storycoffee.co.nz` / `password`
- Customer: `john@aucklandcafe.co.nz` / `password`
- Customer: `sarah@wellingtoncoffee.co.nz` / `password`

## Implemented Flows

- Phase 1: JWT login, Admin order status workflow, Customer order read-only view.
- Phase 2: Admin invoice list, invoice email status transition, payment recording, Customer invoice read-only view.
- Phase 3: Admin statement generation, statement snapshot detail, statement email status transition, Customer statement read-only view.
- Phase 4: Admin production queue aggregation, produced quantity updates, production completion, Ready to Ship order transition.
- Phase 5: Product catalog read API, Admin standing-order list/manual generation, Customer standing-order read/update flow.
- Phase 6: Admin customer create/update/detail, Customer profile read/update with server-side customer isolation.
- Phase 7: Admin/Customer dashboards backed by API data, Admin product create/update flow.
- Phase 8: Invoice/statement PDF metadata generation and authenticated PDF download endpoints.
- Phase 9: AuditLog and EmailLog persistence with Admin log review page.
- Phase 10: Admin log filtering, pagination, and CSV export.
- Phase 11: AuditLog old/new change details for catalog and standing-order updates.
- Phase 12: Admin standing-order pause, resume, and cancel lifecycle actions.
- Phase 13: Admin standing-order create and edit flow.
- Phase 14: Scheduled standing-order generation job with execution logs and manual admin trigger.
- Phase 15: Customer password change with current-password verification.
- Phase 16: Customer invite email flow, dashboard aggregate APIs, product archive action, payment voiding, overdue invoice marking, and formalized invoice PDF content.
- Phase 17: Backend structure cleanup, EF Core migrations, startup migration strategy, and database schema documentation.
- Phase 18: Minimal API routes migrated to Controllers, unified exception middleware, JWT middleware, typed options, and first Repository/UseCase slice.
- Phase 19: Billing, Statement, and Standing Order moved to UseCase + Repository; seed strategy made environment/config controlled; external stubs/jobs split by responsibility.
- Phase 20: ProductionBatch/ProductionItem and InvoiceItem schema, batch-to-production API, production batch/item APIs, local presigned document downloads, and Pending/Sent/Failed email provider flow.
- Phase 21: Redis/Quartz/Serilog/FluentValidation packages wired, `/ready` checks DB/Redis/document storage, local compose adds LocalStack/MailHog, Helm adds Redis and `/api` ingress routing, CI/E2E/k6/Terraform scaffolds added.
- Phase 22: OrderWorkflow, Production, and Catalog moved to UseCase + Repository, `IClock`/`IUnitOfWork`/Outbox added, SMTP/S3/QuestPDF providers wired, API integration tests switched to PostgreSQL/Redis Testcontainers, and legacy `production_progress` removed.
- Phase 23: API integration tests now reset PostgreSQL per test, standing-order dates are normalized to UTC, and the frontend API client uses backend OpenAPI operation contracts.
- Phase 24: Customer-specific price books, customer account lifecycle enforcement, customer effective product pricing, true batch-to-production frontend flow, and confirmation dialogs for destructive admin actions.
- Phase 25: AWS SES email provider support, typed SES configuration, and PostgreSQL `FOR UPDATE SKIP LOCKED` outbox claiming with stale-lock recovery.
- Phase 26: SES webhook intake for Bounce/Complaint/Delivery events, email delivery event persistence, and provider event reconciliation into EmailLog/invoice/statement state.
- Phase 27: Backend split into Api, Contracts, Domain, Application, Infrastructure, and Tests projects with enforced dependency-boundary architecture tests.
- Phase 28: Customer invoice/statement detail APIs and pages, customer portal dashboard/list Query wiring, and customer-only detail authorization coverage.
- Phase 29: Frontend download flows split API-authenticated contract downloads from external presigned downloads, with typed OpenAPI query contracts for CSV exports.
- Phase 30: Non-AWS stability hardening with broader k6 API smoke coverage, Customer portal Playwright detail coverage, Helm lint, Docker Compose config checks, and backend/frontend Docker image build checks in CI.

## Local Development

```bash
pnpm install
pnpm --filter frontend dev
dotnet run --project backend/src/StoryCoffee.Api
```

The frontend uses `/api` through the Vite proxy. The API is expected at `http://localhost:5080` during local development.

## API Contract

```bash
pnpm generate:api
```

The command reads `STORYCOFFEE_OPENAPI_URL` or defaults to `http://localhost:5080/swagger/v1/swagger.json`, then regenerates `frontend/src/shared/api/generated/schema.ts`. Frontend API calls derive response and payload types from the generated OpenAPI `paths` contract.

## Docker Compose

```bash
docker compose -f infra/docker-compose.yml up --build
```

The stack starts PostgreSQL, Redis, the API, and the frontend.
It also starts LocalStack for S3-compatible development and MailHog for local SMTP capture.
Host ports are `8080` for frontend, `5080` for API, `15432` for PostgreSQL, `16379` for Redis, `4566` for LocalStack, and `8025` for MailHog.

For deterministic E2E runs with database reset enabled, start the local stack with the E2E override:

```bash
docker compose -f infra/docker-compose.yml -f infra/docker-compose.e2e.yml up --build
pnpm test:e2e:reset
```

The reset endpoint is disabled by default and only works in Development or Testing when `Testing:ResetEnabled=true`.

## Seed Data

Demo data is controlled by `SeedData` options. Production defaults do not seed test/demo data; development and testing can opt in through `SeedData:EnableInDevelopment`, `SeedData:EnableInTesting`, or explicit `SeedData:Enabled`.

## Document Storage

`DocumentStorage:Provider=Local` writes generated PDFs to `DocumentStorage:LocalRoot` and serves signed `/api/files/download` URLs. `DocumentStorage:Provider=S3` uploads PDFs to S3/MinIO/LocalStack and returns S3 presigned URLs.

## Email Delivery

`Email:Provider=Stub` is safe for local no-op sending, `Email:Provider=Smtp` sends to MailHog or SMTP, and `Email:Provider=SES` sends through AWS SES v2. SES uses `Email:SesRegion`, optional `Email:SesEndpointUrl`, optional `Email:SesConfigurationSet`, and AWS SDK default credentials such as IAM role, web identity, profile, or environment credentials. SES/SNS delivery notifications post to `POST /api/webhooks/ses`; production defaults verify SNS signatures, optionally restrict `Email:SnsTopicArn`, and can auto-confirm subscriptions with `Email:AutoConfirmSnsSubscriptions=true`.

## Validation

```bash
dotnet test backend/StoryCoffee.sln -m:1 /nr:false
dotnet test backend/StoryCoffee.sln -m:1 /nr:false --filter "FullyQualifiedName!~ApiIntegrationTests"
pnpm --filter frontend exec tsc --noEmit
pnpm --filter frontend test
pnpm --filter frontend build
pnpm test:e2e
pnpm test:e2e:reset
pnpm test:perf
pnpm check:api
pnpm smoke:api
helm lint infra/helm/storycoffee
helm template storycoffee infra/helm/storycoffee
docker compose -f infra/docker-compose.yml config
docker compose -f infra/docker-compose.test.yml config
docker build -f backend/src/StoryCoffee.Api/Dockerfile -t storycoffee-api:local .
docker build -f frontend/Dockerfile -t storycoffee-frontend:local .
```

`pnpm check:api` expects the API Swagger endpoint to be running at `STORYCOFFEE_OPENAPI_URL` or `http://localhost:5080/swagger/v1/swagger.json`. `pnpm smoke:api` expects a seeded API at `STORYCOFFEE_API_URL` or `http://localhost:5080`. `pnpm test:e2e` expects the frontend at `E2E_BASE_URL` or `http://localhost:8080` and the API at `E2E_API_BASE_URL` or `http://localhost:5080`. `pnpm test:e2e:reset` also expects the API to run with `Testing:ResetEnabled=true` and matching `E2E_RESET_TOKEN`. `pnpm test:perf` expects k6 plus a seeded API at `API_BASE_URL` or `http://localhost:5080`. `helm` and `docker` must be installed locally for the final checks.

## Engineering Scaffolds

- CI: `.github/workflows/ci.yml`
- E2E: `playwright.config.ts` and `e2e/smoke.spec.ts`
- Performance smoke: `k6/smoke.js`
- AWS/EKS placeholder: `terraform`

## Kubernetes Dev Chart

```bash
helm template storycoffee infra/helm/storycoffee
helm template storycoffee infra/helm/storycoffee -f infra/helm/storycoffee/values-dev.yaml
helm template storycoffee infra/helm/storycoffee -f infra/helm/storycoffee/values-staging.yaml
helm template storycoffee infra/helm/storycoffee -f infra/helm/storycoffee/values-prod.yaml
```

For local Kubernetes deployment with Docker Desktop, see `docs/kubernetes-local.md`.
For AWS staging/production readiness, see `docs/aws-production-checklist.md`.
For backup and restore procedures, see `docs/backup-restore.md`.
