# HouseKeeper

HouseKeeper is a mobile-first household management application built as a .NET modular monolith with a standalone Blazor WebAssembly PWA and ASP.NET Core API.

## Repository status

The repository is currently establishing its architecture walking skeleton.

## Local development

The preferred multi-resource development entry point is the .NET Aspire AppHost:

```bash
dotnet restore HouseKeeper.slnx
dotnet run --project src/HouseKeeper.AppHost
```

This starts the API, PostgreSQL 18.4 and Azurite and exposes the Aspire dashboard for resource state, logs, traces and metrics. See [`docs/development/aspire-local-development.md`](docs/development/aspire-local-development.md) for prerequisites, boundaries and validation steps.

The API remains directly runnable with ordinary configuration; Aspire is not the production deployment model.
