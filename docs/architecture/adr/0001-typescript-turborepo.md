# ADR-0001: Adopt the TypeScript Turborepo architecture

- **Status:** Accepted
- **Date:** 2026-08-26

## Context

HouseKeeper began as an ASP.NET/Blazor modular monolith with PostgreSQL and several planned feature modules. Most modules remained placeholders; Households was the meaningful implemented vertical slice.

The project is being deliberately pivoted to a TypeScript-centric stack using Next.js, PostgreSQL, Tailwind CSS, shadcn/ui, Turborepo, and tRPC.

## Decision

Use:

- pnpm workspaces managed by Turborepo;
- `apps/web` as the Next.js App Router host;
- `packages/api` for tRPC application procedures and domain validation;
- `packages/db` for Drizzle/PostgreSQL persistence;
- app-local shadcn components styled with Tailwind CSS 4;
- a single deployable modular monolith until operational evidence justifies another boundary.

The existing Households domain behavior is ported. Empty framework placeholders are not.

## Consequences

### Positive

- One language across web, application, and likely future infrastructure.
- End-to-end type safety without maintaining a duplicate REST client contract.
- Faster UI iteration with the Next.js/Tailwind/shadcn ecosystem.
- Package boundaries remain explicit enough to prevent database logic leaking into React.
- Turborepo provides task orchestration without forcing independent deployments.

### Costs

- Existing C# implementation and infrastructure code are retired rather than incrementally reused.
- Authentication and deployment adapters must be rebuilt for the new runtime.
- Database migration ownership moves from EF Core to Drizzle.
- Team conventions must explicitly protect server/client boundaries because TypeScript makes cross-package imports easy.

## Guardrails

- No direct database imports from React components.
- No client-provided identity is trusted in production.
- Every household-scoped operation checks membership server-side.
- Production database changes use reviewed migrations, not schema push.
- Add workspace packages only for a real ownership/reuse boundary.
