# ADR-0002: Baseline the existing PostgreSQL schema for Drizzle

- **Status:** Proposed
- **Date:** 2026-08-26

## Context

The existing Households database was created by EF Core. Its deployed physical contract is PostgreSQL schema `households` containing `households` and `household_members`, with EF's migration history stored separately.

The TypeScript migration maps Drizzle to that physical contract so the application can read and write existing data without creating parallel tables.

However, matching tables does not synchronize migration histories. A first `drizzle-kit generate` run starts from Drizzle's empty snapshot and can therefore emit create-schema/create-table statements that are inappropriate for an already-provisioned EF database.

Drizzle's documented database-first flow provides `drizzle-kit pull --init`: it introspects an existing database, generates schema/migration metadata, and marks that pulled state as the initial applied migration. That is the correct mechanism to evaluate for this cutover, but it still needs to be verified against HouseKeeper's existing EF-created schema and the pinned Drizzle Kit version before it is used on a shared environment.

## Proposed decision

Before any shared-development or production environment runs a Drizzle migration:

1. verify the deployed EF schema matches the checked-in Drizzle model structurally;
2. take and verify a restorable PostgreSQL backup;
3. clone or restore the shared-development schema into an isolated migration-validation database;
4. run `drizzle-kit pull --init` against that isolated existing database and review the generated schema, SQL, metadata snapshot, and migration-journal effects;
5. compare the introspected output with `packages/db/src/schema.ts`, including PostgreSQL schema, quoted column names, key/constraint names, indexes, foreign keys, role casing, and timestamps;
6. establish the reviewed introspected snapshot as the Drizzle baseline without recreating existing objects;
7. prove a fresh empty database can reach the same physical shape through the accepted migration path;
8. prove an EF-created database can enter the Drizzle migration stream and apply a subsequent no-data-loss test migration cleanly;
9. retain the EF migration history table as historical evidence until a later cleanup decision.

Only after those checks should this ADR be changed from Proposed to Accepted and the exact baseline procedure be added to the deployment runbook.

## Guardrail until accepted

- `pnpm db:push` is permitted only for disposable/local review databases.
- `pnpm db:generate` may be used to inspect generated SQL, but its initial output must not be applied to an existing environment.
- `drizzle-kit pull --init` must first be exercised against an isolated clone/restore, not production.
- No deployment workflow may run Drizzle migrations against shared-development or production yet.

## Consequences

This creates a deliberate migration cutover instead of pretending ORM replacement is only a source-code concern. It also lets the application migration proceed independently from the deployment decision while preserving existing household data and establishing Drizzle's migration history from the database that actually exists.
