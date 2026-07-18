# HouseKeeper

HouseKeeper is a mobile-first household management application. This repository currently contains the HK-14 architecture walking skeleton: an installable Blazor WebAssembly PWA, an ASP.NET Core API, and a PostgreSQL-backed Households module.

## What the walking skeleton proves

A development user can:

1. open the PWA;
2. establish a stable local development identity;
3. authenticate that identity through the API middleware;
4. create a household and owner membership in one transaction;
5. restart the API and browser;
6. load the same household from PostgreSQL.

The development identity is intentionally not production authentication. It is enabled only in the `Development` environment and exists to exercise the same claims, authorization, API, and persistence boundaries that an external identity provider will use later.

## Prerequisites

- .NET SDK `10.0.300` or a compatible feature-band roll-forward
- Docker with Docker Compose
- Bash on macOS/Linux, or PowerShell 7 on Windows

The .NET SDK, NuGet packages, and `dotnet-ef` tool are pinned by repository manifests.

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

## Run validation manually

With the local API running:

```bash
bash scripts/smoke.sh
```

To run the complete compile and test checks:

```bash
dotnet tool restore
dotnet restore HouseKeeper.slnx
dotnet build HouseKeeper.slnx --configuration Release --no-restore
dotnet test HouseKeeper.slnx --configuration Release --no-build
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
  Blazor WebAssembly PWA
  local development session
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
  owner membership authorization boundary
  EF Core module context and migrations
         |
         v
PostgreSQL 18.4
  households schema
```

The module owns its schema and migration history. Other business modules must not access its tables directly.

## Continuous integration

The pull-request workflow:

1. restores the pinned SDK tools and NuGet graph;
2. builds with nullable analysis, recommended analyzers, and warnings as errors;
3. runs xUnit v3 through Microsoft Testing Platform v2;
4. publishes the API and PWA;
5. applies migrations to a clean PostgreSQL 18.4 service;
6. starts the published API;
7. runs the authenticated household smoke journey;
8. uploads test and API diagnostic artifacts.
