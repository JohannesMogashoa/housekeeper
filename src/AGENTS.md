# Codex instructions for production source

These instructions apply to all production code under `src/` in addition to the repository root `AGENTS.md`.

## Dependency direction

- Module implementations do not reference other module implementations, infrastructure namespaces, entities, repositories, or `DbContext` types.
- API code performs composition, authentication/authorization plumbing, endpoint mapping, and transport adaptation only.
- Domain namespaces do not reference ASP.NET Core, EF Core, browser APIs, storage/provider SDKs, endpoint types, or configuration binding.
- Public contracts are purpose-specific, immutable or serialization-safe, and dependency-light.
- New shared abstractions require proven multi-module semantics, not superficial duplication.

## Domain and application behavior

- Enforce invariants in domain/application code so another endpoint or client cannot bypass them.
- Reject invalid and concurrent state transitions deterministically.
- Use the approved clock/time abstractions and preserve IANA timezone snapshots where local time carries business meaning.
- Do not rewrite historical state when definitions or schedules change.
- Return stable business outcomes and Problem Details reason codes rather than leaking exception text.
- Verify authorization before loading or mutating household-scoped business state.

## EF Core and PostgreSQL

- New persistent state has one owning module, schema, `DbContext`, migration history, indexes, and constraints.
- Migrations are deterministic and safe for the deployment order; destructive changes use expand-and-contract or explicit operational handling.
- Runtime startup does not apply production migrations.
- Transaction boundaries include all local state and outbox rows that must commit atomically.
- Cross-module IDs have no ORM navigation properties or cross-schema cascades.
- Queries are bounded, cancellable, household-scoped, and avoid accidental N+1 behavior or unbounded materialization.
- Use database constraints for concurrency and uniqueness where application checks alone race.

## Reliability

- Replayable commands use operation identity and request fingerprint rules consistently.
- Test duplicate requests, response loss, concurrent execution, and stale authorization.
- Events are immutable past-tense facts with stable IDs and payload versions.
- Workers release database transactions before external calls and implement lease recovery, retry classification, backoff, terminal state, cancellation, and graceful shutdown.
- Logs and metrics use stable categories without sensitive payloads or uncontrolled high-cardinality dimensions.

## Review threshold

Treat missing cross-household, concurrency, migration, idempotency, or restart coverage as material when the change affects those risks. Prefer assertions on externally meaningful state and behavior over mocks of implementation details.
