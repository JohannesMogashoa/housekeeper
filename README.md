# HouseKeeper

HouseKeeper is a mobile-first household management application. This repository currently contains the HK-14 architecture walking skeleton and the HK-28 AWS platform foundation: an installable Blazor WebAssembly PWA, an ASP.NET Core API, PostgreSQL-backed modules, and reviewable AWS CDK/container definitions.

## Project documentation

- [Release promotion runbook](docs/development/release-promotion.md) - branch rules, workflow triggers, approvals, artifacts and recovery

- [Technical recommendation](docs/architecture/technical-recommendation.md) — final stack, architecture, dependency rules, risks and deferred decisions
- [Architecture decision index](docs/architecture/adr/README.md) — accepted ADR catalogue and implementation status
- [Foundation backlog](docs/foundation-backlog.md) — ordered execution plan following Discovery 0
- [Local development guide](docs/development/local-development.md) — prerequisites, startup, migrations, tests and troubleshooting
- [Codex pull-request review](docs/development/pr-review-agent.md) — repository instructions, on-demand review and automatic-review activation
- [AWS deployment foundation](deploy/aws/README.md) — CDK configuration, synth, security checks, container build and protected deployment boundaries

## What the walking skeleton proves

A development user can:

1. open the published PWA;
2. establish a stable local development identity;
3. authenticate that identity through ASP.NET Core middleware;
4. create a household and owner membership in one transaction;
5. restart both the browser and API process;
6. load the same household from PostgreSQL.

The development identity is intentionally not production authentication. It is enabled only in the `Development` environment and exists to exercise the same claims, authorization, API, and persistence boundaries that an external identity provider will use later.

## Prerequisites

- .NET SDK `10.0.300` or a compatible feature-band roll-forward
- Docker with Docker Compose
- Bash on macOS/Linux, or PowerShell 7 on Windows
- Node.js 18+ for the AWS CDK CLI when synthesizing infrastructure

The .NET SDK, NuGet packages, and `dotnet-ef` tool are pinned by repository manifests. NuGet vulnerability auditing remains enforced during restore.

## Start locally

### macOS or Linux

```bash
bash scripts/dev.sh
```

### Windows

```powershell
pwsh ./scripts/dev.ps1
```

The command:

- starts PostgreSQL 18.4 on local port `54329`;
- restores repository tools and packages;
- waits for PostgreSQL readiness;
- applies the Households module migrations explicitly;
- starts the API at `http://localhost:5287`;
- starts the PWA at `http://localhost:5136`.

Open `http://localhost:5136`, enter a display name, and create a household. Stop the application processes with `Ctrl+C`. The PostgreSQL Docker volume remains, so the household will still appear after the next startup.

To reset all local household data:

```bash
docker compose -f deploy/local/compose.yaml down --volumes
```

## Test portfolio

The walking skeleton establishes four test layers:

| Layer | Tooling | Current responsibility |
|---|---|---|
| Domain | xUnit v3 on Microsoft Testing Platform v2 | Household-name invariants |
| Component | bUnit 2 | Empty and populated household rendering |
| Architecture | ArchUnitNET plus reflection assertions | Module and dependency boundaries |
| End-to-end | Playwright Chromium | Published PWA, authentication, creation, reload, and persistence |

Code coverage is collected through `Microsoft.Testing.Extensions.CodeCoverage` and emitted as Cobertura XML.

### Build and fast tests

```bash
dotnet tool restore
dotnet restore HouseKeeper.slnx
dotnet build HouseKeeper.slnx --configuration Release --no-restore

dotnet test tests/Modules/HouseKeeper.Modules.Households.Tests/HouseKeeper.Modules.Households.Tests.csproj \
  --configuration Release \
  --no-build \
  -- \
  --coverage \
  --coverage-output artifacts/test-results/households.cobertura.xml \
  --coverage-output-format cobertura

dotnet test tests/HouseKeeper.Web.Tests/HouseKeeper.Web.Tests.csproj \
  --configuration Release \
  --no-build
dotnet test tests/HouseKeeper.ArchitectureTests/HouseKeeper.ArchitectureTests.csproj \
  --configuration Release \
  --no-build
```

### API smoke journey

With the local API running:

```bash
bash scripts/smoke.sh
```

### Browser journey

With the local application running, build the end-to-end project and install its pinned Chromium binary once:

```bash
dotnet build tests/HouseKeeper.EndToEndTests/HouseKeeper.EndToEndTests.csproj \
  --configuration Release
pwsh tests/HouseKeeper.EndToEndTests/bin/Release/net10.0/playwright.ps1 \
  install --with-deps chromium
HOUSEKEEPER_WEB_BASE_URL=http://localhost:5136 \
  dotnet test tests/HouseKeeper.EndToEndTests/HouseKeeper.EndToEndTests.csproj \
    --configuration Release \
    --no-build
```

## Apply migrations explicitly

```bash
dotnet ef database update \
  --project src/Modules/HouseKeeper.Modules.Households \
  --startup-project src/HouseKeeper.Api \
  --context HouseholdsDbContext
```

The API does not mutate production schemas during startup. Migration execution remains an explicit developer or deployment-pipeline responsibility.

## Current architecture

```text
HouseKeeper.Web
  standalone Blazor WebAssembly PWA
  browser-local development session
  typed HTTP client
         |
         | development identity headers
         v
HouseKeeper.Api
  authentication and authorization middleware
  trace propagation and structured logging
  liveness and readiness endpoints
         |
         v
HouseKeeper.Modules.Households
  household-name invariant
  create/list use cases
  application-owned membership authorization boundary
  EF Core module context and migrations
         |
         v
PostgreSQL 18.4
  households schema
```

The module owns its schema and migration history. Other business modules must not access its tables directly.

## Continuous integration

Development pull requests use the deliberately small
`.github/workflows/ci.yml` path. It restores dependencies, builds the solution,
runs the deterministic test projects, applies migrations to the temporary
PostgreSQL service needed by the smoke environment, starts the API, and runs
the API smoke journey. It does not publish application output, upload
artifacts, build a container, synthesize CDK, use AWS credentials, or deploy.

Pull requests targeting `release/**` or `master` use
`.github/workflows/ci-release.yml`, which calls the full reusable validation
workflow. Pushes to `development`, `release/**`, and `master` also use the
full workflow. That path retains coverage, application output, Playwright,
restart/persistence checks, strict CDK synthesis and policy checks, and the
release candidate artifact behavior required by promotion.

## AWS platform foundation

The approved cloud platform is AWS in `af-south-1` (Africa/Cape Town). The PWA is hosted from private S3 behind CloudFront Origin Access Control; the API runs as a non-root ASP.NET Core container on ECS Fargate behind an ALB; PostgreSQL uses RDS; authentication uses Cognito User Pools; and delivery uses C# AWS CDK plus GitHub Actions OIDC.

The CDK project is intentionally deployment-safe by default. It requires explicit environment configuration for shared or production resources, protects stateful resources, keeps migration privileges separate from runtime privileges, and never places AWS credentials or privileged secrets in the PWA.

## Branch promotion and deployment

HouseKeeper uses protected promotion rather than deploying ordinary development
pushes to AWS:

```text
feature/* -> development -> rc/vX.Y.Z -> release/vX.Y.Z -> master -> vX.Y.Z
```

| Event | Result |
|---|---|
| PR to `development` | Lightweight build, tests, migration-backed API smoke, and an idempotent Codex review request; no publishing, artifacts, AWS credentials, or deployment |
| PR to `release/**` or `master` | Full reusable validation; no AWS credentials or deployment |
| Push to `development` | Full reusable validation only; no AWS credentials or deployment |
| Protected `rc/vX.Y.Z` tag | Verifies ancestry from `development`, creates `release/vX.Y.Z`, and opens one promotion PR to `master` |
| Release PR | Full validation; application-only changes skip AWS; infrastructure or migration changes require an explicitly enabled protected shared-development pre-production run |
| Merge of `release/vX.Y.Z` into `master` | Finds the successful candidate artifact for the exact release head, deploys it to protected `production`, then creates `vX.Y.Z` and the GitHub Release |
| Failed deployment | No final release tag is created; use the protected retry/rollback procedure in the [release promotion runbook](docs/development/release-promotion.md) |

`development` is AWS-free. AWS deployment occurs only in the protected
`shared-development` environment for an opted-in infrastructure pre-production
run, or in the protected `production` environment after a reviewed release PR
merges. Both environments reuse the environment-specific CDK stacks and
serialize deployment jobs; shared-development is one deliberately reused
environment, not a per-branch account or stack.

The release candidate artifact is produced by the successful release-branch
validation (push, or the promotion workflow's dispatch when the branch is
created through the API). It contains the published API/PWA, API image bytes, source
SHA, source tree SHA, release version, and image identity. Deployment verifies
all of those values and uses the image by ECR digest. The PWA receives only
public endpoint/client configuration at upload time; it is not rebuilt.

Human controls remain required: branch rules require reviewed PRs and no force
pushes for `development`, `release/**`, and `master`; `rc/*` and final release
tags are protected; the shared-development and production GitHub environments
require their configured reviewers; and material Codex findings must be
resolved or explicitly dispositioned. The protected environment variables,
OIDC trust, and exact setup commands are documented in the [release promotion
runbook](docs/development/release-promotion.md) and [AWS deployment guide](deploy/aws/README.md).
