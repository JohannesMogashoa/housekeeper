# HouseKeeper

HouseKeeper is a mobile-first household management application. This repository currently contains the HK-14 architecture walking skeleton: an installable Blazor WebAssembly PWA, an ASP.NET Core API, and a PostgreSQL-backed Households module.

## Project documentation

- [Technical recommendation](docs/architecture/technical-recommendation.md) — final stack, architecture, dependency rules, risks and deferred decisions
- [Architecture decision index](docs/architecture/adr/README.md) — accepted ADR catalogue and implementation status
- [Foundation backlog](docs/foundation-backlog.md) — ordered execution plan following Discovery 0
- [Local development guide](docs/development/local-development.md) — prerequisites, startup, migrations, tests and troubleshooting

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

The pull-request workflow:

1. restores pinned tools and the vulnerability-audited NuGet graph;
2. builds the complete solution with nullable analysis, recommended analyzers, and warnings as errors;
3. runs domain, bUnit, and architecture tests through Microsoft Testing Platform v2;
4. collects and retains Cobertura coverage;
5. publishes the API and PWA and rejects unresolved static-asset fingerprints;
6. applies migrations to a clean PostgreSQL 18.4 service;
7. starts both published applications;
8. runs the authenticated API smoke journey;
9. installs pinned Playwright Chromium and executes the real browser journey;
10. restarts the published API and verifies the original household remains available;
11. uploads restore, build, test, migration, API, web, browser, and coverage artifacts.

The workflow does not deploy production infrastructure.
