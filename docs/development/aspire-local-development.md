# Aspire local development POC (HK-12)

## Purpose

This spike evaluates .NET Aspire as a local orchestration and observability layer for HouseKeeper. It does not select Aspire as the production deployment model.

## Resources

The AppHost starts:

- `HouseKeeper.Api` as the modular-monolith backend and background-job host;
- PostgreSQL 18.4 with a persistent development volume and a `housekeeper` database;
- Azurite-backed Azure Blob Storage with a persistent development volume and an `attachments` blob resource;
- the Aspire dashboard for resource state, structured logs, traces and metrics.

The standalone Blazor WebAssembly PWA remains independently runnable during this spike. It will be added to the AppHost only when its API base-address and static-development-server behavior are implemented deliberately in the walking skeleton.

## Prerequisites

- .NET 10 SDK from the repository `global.json` line;
- Docker Desktop, Podman Desktop or another Aspire-supported OCI container runtime;
- trusted local ASP.NET Core development certificates where HTTPS is used.

## Run

```bash
dotnet restore HouseKeeper.slnx
dotnet run --project src/HouseKeeper.AppHost
```

Open the dashboard URL printed by the AppHost. Confirm that PostgreSQL, Azurite and the API reach a healthy state, then inspect:

- API console and structured logs;
- `/health` readiness and `/alive` liveness endpoints;
- HTTP request traces for `/weatherforecast`;
- runtime and HTTP metrics;
- injected connection-string configuration for `housekeeper` and `attachments`.

## Reset local data

The POC uses named container volumes:

- `housekeeper-postgres-data`
- `housekeeper-azurite-data`

Delete those volumes explicitly when a clean local state is required. Normal AppHost shutdown must not delete developer data.

## Boundaries

- Aspire AppHost is development tooling and a composition model, not a business module.
- Production deployment must not require the AppHost or Aspire dashboard.
- Module code must consume normal .NET configuration and module-local abstractions, not AppHost resource types.
- Integration tests continue to own their Testcontainers lifecycle rather than depending on a developer AppHost.
- PostgreSQL migrations remain explicit repository workflows and are not silently applied by Aspire.
- Secrets stay in user secrets or environment-specific secret stores and are never committed.

## POC validation checklist

- [ ] Solution restores and builds under the approved .NET 10 SDK.
- [ ] AppHost starts with an OCI container runtime available.
- [ ] PostgreSQL reports healthy and retains data across restart.
- [ ] Azurite reports healthy and retains blobs across restart.
- [ ] API waits for both resources and reaches healthy state.
- [ ] Dashboard receives logs, traces and metrics from the API.
- [ ] API runs directly without AppHost when supplied ordinary configuration.
- [ ] No Aspire hosting package is referenced by a business module.

The walking-skeleton task should execute and automate these checks before merging the POC into the main branch.
