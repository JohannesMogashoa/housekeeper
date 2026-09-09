# HouseKeeper

HouseKeeper is a mobile-first household management application built as a production-oriented .NET system. The repository currently contains a working household-management vertical slice, an installable Blazor WebAssembly PWA, an ASP.NET Core API, PostgreSQL persistence, automated test coverage, and an AWS deployment foundation.

> **Current status:** the implemented walking skeleton proves household creation, membership ownership, persistence, reload, authorization boundaries, CI validation, and deployable infrastructure. Broader household-management features are still being built on top of this foundation.

## What is implemented

A development user can currently:

1. open the installable PWA;
2. establish a stable local development identity;
3. authenticate through the ASP.NET Core middleware boundary;
4. create a household and owner membership in one transaction;
5. restart the browser and API;
6. load the same household from PostgreSQL.

The development identity is intentionally limited to the `Development` environment. It exercises the same claims and authorization boundaries intended for a production identity provider without pretending production authentication is already complete.

## Engineering highlights

- **Modular .NET architecture** with business modules owning their own schema and migration history.
- **Blazor WebAssembly PWA** backed by a typed ASP.NET Core API.
- **PostgreSQL + EF Core** with explicit migration execution rather than production startup mutation.
- **Layered testing** across domain, component, architecture, API smoke, and Playwright browser journeys.
- **AWS CDK infrastructure** for CloudFront/S3, ECS Fargate, RDS, Cognito, and supporting networking/security resources.
- **GitHub Actions delivery pipeline** with separate lightweight PR validation and protected release validation/deployment paths.
- **OIDC-based AWS delivery** instead of long-lived AWS credentials in GitHub.

## Architecture

```text
HouseKeeper.Web
  Blazor WebAssembly PWA
  browser-local development session
  typed HTTP client
         |
         v
HouseKeeper.Api
  authentication + authorization
  tracing + structured logging
  health endpoints
         |
         v
HouseKeeper.Modules.Households
  household domain rules
  create/list use cases
  membership authorization
  EF Core module context + migrations
         |
         v
PostgreSQL
  households-owned schema
```

Other modules are expected to interact through application boundaries rather than reaching directly into another module's tables.

## Tech stack

| Area | Technology |
| --- | --- |
| Web | Blazor WebAssembly PWA |
| API | ASP.NET Core |
| Runtime | .NET 10 |
| Persistence | PostgreSQL + EF Core |
| Tests | xUnit v3, bUnit, ArchUnitNET, Playwright |
| Local infrastructure | Docker Compose |
| Cloud | AWS: S3, CloudFront, ECS Fargate, ALB, RDS, Cognito |
| Infrastructure as code | AWS CDK in C# |
| Delivery | GitHub Actions + AWS OIDC |

## Repository documentation

- [Technical recommendation](docs/architecture/technical-recommendation.md) — architecture, dependency rules, risks, and deferred decisions
- [ADR index](docs/architecture/adr/README.md) — accepted architecture decisions
- [Foundation backlog](docs/foundation-backlog.md) — ordered implementation work
- [Local development guide](docs/development/local-development.md) — setup, migrations, tests, and troubleshooting
- [Codex PR review](docs/development/pr-review-agent.md) — automated review workflow
- [AWS deployment guide](deploy/aws/README.md) — infrastructure configuration and deployment boundaries
- [Release promotion runbook](docs/development/release-promotion.md) — branch promotion, approvals, artifacts, rollback, and recovery

## Prerequisites

- .NET SDK `10.0.300` or compatible feature-band roll-forward
- Docker with Docker Compose
- Bash on macOS/Linux or PowerShell 7 on Windows
- Node.js 18+ when using the AWS CDK CLI

## Run locally

### macOS / Linux

```bash
bash scripts/dev.sh
```

### Windows

```powershell
pwsh ./scripts/dev.ps1
```

The development scripts start PostgreSQL, restore tools/packages, apply the Households migrations, and start:

- API: `http://localhost:5287`
- PWA: `http://localhost:5136`

Reset the local database with:

```bash
docker compose -f deploy/local/compose.yaml down --volumes
```

## Build and test

```bash
dotnet tool restore
dotnet restore HouseKeeper.slnx
dotnet build HouseKeeper.slnx --configuration Release --no-restore
```

The repository contains separate domain, component, architecture, API smoke, and end-to-end test projects. See the [local development guide](docs/development/local-development.md) for the complete commands.

Apply the module migrations explicitly with:

```bash
dotnet ef database update \
  --project src/Modules/HouseKeeper.Modules.Households \
  --startup-project src/HouseKeeper.Api \
  --context HouseholdsDbContext
```

## Delivery model

HouseKeeper promotes changes through protected branches/tags rather than deploying ordinary development pushes directly to AWS:

```text
feature/* -> development -> rc/vX.Y.Z -> release/vX.Y.Z -> master -> vX.Y.Z
```

Development PRs use a deliberately lightweight CI path. Release branches and production promotion use the fuller validation path, release artifacts, protected GitHub environments, AWS OIDC, and explicit deployment controls.

## Project direction

The current repository is intentionally foundation-heavy: the goal is to establish durable module boundaries, persistence, authentication/authorization seams, testing, and delivery before adding broader household features such as shared tasks, schedules, inventory, reminders, and other home-management workflows.
