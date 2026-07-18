# ADR 0009: Adopt .NET Aspire for local orchestration and development observability

- Status: Proposed by HK-12
- Date: 2026-07-18

## Decision

HouseKeeper will adopt .NET Aspire narrowly as the preferred local orchestration and development-observability entry point.

The AppHost composes the ASP.NET Core API, PostgreSQL and Azurite. Aspire ServiceDefaults supplies OpenTelemetry, development health endpoints, service discovery and standard outbound HTTP resilience. Aspire is not the production deployment model and does not own database migrations, business configuration or module boundaries.

## Rationale

The local system already requires multiple coordinated processes and emulators. Aspire provides one executable topology, readiness ordering, resource configuration injection and a dashboard for logs, traces and metrics without introducing a production broker, scheduler or platform runtime.

The modular monolith still deploys as one API process. Background jobs remain hosted in that process for the MVP, PostgreSQL remains the durable source of truth and object storage remains behind the Attachments module.

## Constraints

- A supported OCI container runtime is required for the full local topology.
- The API must remain directly runnable without the AppHost.
- Business modules must not reference Aspire hosting packages or AppHost resource types.
- Integration tests continue to use Testcontainers and controlled fakes.
- Production infrastructure remains independently defined and deployable.
- The standalone Blazor WebAssembly project is added to AppHost only after its API routing model is implemented deliberately.

## Consequences

Positive:

- one command starts the local API and infrastructure;
- resource readiness and connection configuration are explicit;
- development logs, traces and metrics are visible immediately;
- local PostgreSQL and Azurite match the selected production technologies;
- future worker extraction can be represented without changing module contracts.

Negative:

- two additional development projects and several telemetry packages are introduced;
- contributors need Docker, Podman or another supported container runtime;
- Aspire package updates add another servicing stream;
- generated ServiceDefaults must be reviewed rather than treated as invisible template code.

## Revisit triggers

Reconsider this decision when Aspire causes persistent restore or IDE friction, prevents direct API execution, produces package-version conflicts with .NET 10, or fails to reduce local setup and diagnosis time during the walking skeleton.
