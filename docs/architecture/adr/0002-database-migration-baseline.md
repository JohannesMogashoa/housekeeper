# ADR-0002: Baseline the existing PostgreSQL schema for Drizzle

- **Status:** Proposed
- **Date:** 2026-08-26

## Context

The existing Households database was created by EF Core. Its deployed physical contract is PostgreSQL schema `households` containing `households` and `household_members`, with EF's migration history stored separately.

The TypeScript migration maps Drizzle to that physical contract so the application can read and write existing data without creating parallel tables.

However, matching tables does not synchronize migration histories. A first `drizzle-kit generate` run starts from Drizzle's empty snapshot and can therefore emit create-schema/create-table statements that are inappropriate for an already-provisioned EF database.

## Proposed decision

Before any shared-development or production environment runs a Drizzle migration:

1. verify the deployed EF schema matches the Drizzle model structurally;
2. take and verify a restorable PostgreSQL backup;
3. create a reviewed Drizzle baseline snapshot/migration representing the existing schema;
4. mark only that verified baseline as already applied in existing environments using Drizzle's supported migration journal mechanism;
5. prove a fresh empty database can reach the same shape from the baseline path;
6. prove an existing EF-created database can enter the Drizzle migration stream without recreating or dropping objects;
7. retain the EF migration history table as historical evidence until a later cleanup decision.

The exact baseline/journal commands must be validated against the pinned Drizzle Kit version before this ADR is accepted.

## Guardrail until accepted

- `pnpm db:push` is permitted only for disposable/local review databases.
- `pnpm db:generate` may be used to inspect generated SQL, but its initial output must not be applied to an existing environment.
- No deployment workflow may run Drizzle migrations against shared-development or production yet.

## Consequences

This creates a deliberate migration cutover instead of pretending ORM replacement is only a source-code concern. It also lets the application migration proceed independently from the deployment decision while preserving the option to reuse existing household data safely.
