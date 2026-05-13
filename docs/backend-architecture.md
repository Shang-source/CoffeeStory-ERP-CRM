# StoryCoffee Backend Architecture

The backend is organized as a bounded .NET solution. Dependency direction is intentionally one-way:

```text
StoryCoffee.Api
  -> StoryCoffee.Infrastructure
  -> StoryCoffee.Application
  -> StoryCoffee.Contracts
  -> StoryCoffee.Domain
```

`StoryCoffee.Contracts` can reference `StoryCoffee.Domain` for stable enums. `StoryCoffee.Application` can reference `StoryCoffee.Contracts` and `StoryCoffee.Domain`. `StoryCoffee.Infrastructure` implements application ports and can reference all lower layers. `StoryCoffee.Api` is the composition root and HTTP adapter.

## Projects

- `backend/src/StoryCoffee.Api` - HTTP controllers, middleware, validation filters, Swagger, and dependency injection composition.
- `backend/src/StoryCoffee.Contracts` - request/response contracts that define the public API boundary.
- `backend/src/StoryCoffee.Domain` - persisted domain entities, enums, and durable business state.
- `backend/src/StoryCoffee.Application` - use cases, repository ports, provider interfaces, mapping, error contracts, clock/unit-of-work/outbox abstractions, and application-level DI registrations.
- `backend/src/StoryCoffee.Infrastructure` - EF Core context, migrations, repositories, auth, document/email providers, Quartz jobs, Redis/S3/SES/SMTP integrations, typed options, and infrastructure DI registrations.
- `backend/tests/StoryCoffee.Tests` - unit, workflow, integration, and dependency-boundary tests.

## Bounded Modules

Application and Infrastructure are organized by durable business modules under `Modules`:

- `Auth` - login, password changes, password hashing, JWT issuing, and user repository ports/implementations.
- `Customers` - customer profile, admin customer lifecycle, archive blockers, invite workflow, and customer audit changes.
- `Catalog` - product catalog lifecycle, customer-visible effective prices, customer price books, and future standing-order repricing.
- `Orders` - order state workflow, order repository port, and order DTO mapping.
- `StandingOrders` - standing order editing, lifecycle actions, generation job contract, Quartz job implementation, and job execution mapping.
- `Production` - production batch/item workflows and repository implementation.
- `Billing` - invoices, payments, overdue rules, invoice document actions, and billing repository implementation.
- `Statements` - statement generation/sending/download workflows and statement repository implementation.
- `Emails` - outbound email provider ports, SES/SMTP/stub implementations, SES/SNS webhook intake, and delivery-event reconciliation.
- `Documents` - PDF generation, S3/local storage, presigned download metadata, storage health checks, and export rendering.
- `Audit`, `Dashboard`, and `Health` - read models, aggregate dashboard queries, and infrastructure readiness checks.

## Composition

- `StoryCoffee.Api.Extensions.AddStoryCoffeeApi` owns HTTP-only wiring: controllers, JSON enum handling, validation filters, Swagger, CORS, and calls into lower-layer DI.
- `StoryCoffee.Application.DependencyInjection.AddStoryCoffeeApplication` registers use cases only; it must not register EF Core, providers, hosted workers, or package-specific infrastructure.
- `StoryCoffee.Infrastructure.DependencyInjection.AddStoryCoffeeInfrastructure` registers persistence, repositories, auth providers, email/PDF/storage providers, Redis/S3/SES/SMTP, Quartz, outbox workers, and typed infrastructure options.

## Namespace Roots

- Domain entities use `StoryCoffee.Domain`.
- API DTOs and public request/response contracts use `StoryCoffee.Contracts`.
- Use cases, ports, mapping, outbox contracts, and application exceptions use `StoryCoffee.Application.*`.
- EF Core, migrations, provider implementations, jobs, and provider options use `StoryCoffee.Infrastructure.*`.
- HTTP controllers, middleware, validation, API-only options, and startup extensions use `StoryCoffee.Api.*`.
- Test files are grouped by the same modules under `backend/tests/StoryCoffee.Tests`.

## Rules

- Domain must not depend on Application, Infrastructure, or Api.
- Contracts must not depend on Application, Infrastructure, or Api.
- Application must not depend on Infrastructure or Api.
- Application must not reference EF Core, Npgsql, Redis, S3, SMTP, SES, Quartz, ASP.NET hosting, or provider-specific infrastructure packages.
- Infrastructure must not depend on Api.
- Api owns hosting and composition only; business orchestration belongs in Application use cases or explicitly infrastructure-backed adapters.
