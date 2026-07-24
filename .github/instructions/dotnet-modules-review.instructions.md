---
applyTo: "**/*.cs,**/*.csproj,**/*.props,**/*.targets,**/*.slnx"
---

# .NET and module review instructions

When reviewing matching files, verify the following in addition to repository-wide instructions.

## Dependency direction

- Module implementations do not reference other module implementations, infrastructure namespaces, entities, repositories, or `DbContext` types.
- API code performs composition, authentication/authorization plumbing, endpoint mapping, and transport adaptation only.
- Domain namespaces do not reference ASP.NET Core, EF Core, browser APIs, storage/provider SDKs, endpoint types, or configuration binding.
- Public contracts are purpose-specific, immutable/serialization-safe, and dependency-light.
- New shared abstractions are justified by proven multi-module semantics, not by superficial duplication.

## Domain and application behavior

- Invariants are enforced in domain/application code and cannot be bypassed through another endpoint or client.
- State transitions reject invalid and concurrent transitions deterministically.
- Time uses the approved abstractions and preserves IANA timezone snapshots where business meaning depends on local time.
- Historical state is not rewritten when definitions or schedules change.
- Commands return stable business outcomes and Problem Details reason codes rather than exception text.

## EF Core and PostgreSQL

- New persistent state has one owning module, schema, `DbContext`, migration history, constraints, and indexes.
- Migrations are deterministic and safe for the deployment order; destructive changes use expand-and-contract or explicit operational handling.
- Runtime startup does not apply production migrations.
- Transaction boundaries include all local state and outbox rows that must commit atomically.
- Cross-module IDs have no ORM navigation properties or cross-schema cascades.
- Queries are bounded, cancellable, household-scoped, and avoid accidental N+1 behavior or unbounded materialization.
- Concurrency and uniqueness rely on database enforcement as well as application checks where races are possible.

## Reliability

- Replayable commands use operation identity and request fingerprint rules consistently.
- Duplicate requests, response loss, concurrent execution, and stale authorization are tested.
- Events are immutable past-tense facts with stable IDs and payload versions.
- Workers release database transactions before external calls and implement lease recovery, retry classification, backoff, terminal state, cancellation, and graceful shutdown.
- Logs and metrics use stable categories and avoid sensitive payloads/high-cardinality dimensions.

## Tests

Require the risk-appropriate mix of domain tests, real PostgreSQL integration tests, API/authorization tests, architecture tests, failure injection, and restart tests. Prefer assertions on externally meaningful state and behavior over mocks of implementation details.

Treat missing cross-household, concurrency, migration, or restart coverage as material findings when the change affects those risks.