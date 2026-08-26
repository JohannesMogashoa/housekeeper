# HouseKeeper engineering guidance

## Architecture

HouseKeeper is a TypeScript modular monolith in a pnpm/Turborepo workspace.

- `apps/web` owns the Next.js application, route handlers, presentation, and app-local shadcn components.
- `packages/api` owns tRPC routers, input validation, authorization boundaries, and application orchestration.
- `packages/db` owns PostgreSQL access and Drizzle schema definitions.
- `infra/local` owns local infrastructure only.

Keep domain features vertically coherent. A feature router may orchestrate persistence, but React components must not query PostgreSQL directly.

## Engineering rules

- TypeScript strict mode is mandatory. Do not introduce `any` to bypass a design problem.
- Validate untrusted inputs at the tRPC boundary with Zod.
- Authorization is server-side. UI visibility is not an authorization control.
- Every household-scoped query must constrain access through household membership.
- Preserve transactions for multi-write invariants such as household + owner-member creation.
- Keep shadcn components copy-owned under `apps/web/components/ui`; product components belong outside that directory.
- Prefer server components by default. Add `"use client"` only where browser state, effects, or client-side tRPC hooks require it.
- Database schema changes require an explicit migration plan before production deployment.

## Validation

Run from the repository root:

```bash
pnpm typecheck
pnpm test
pnpm build
```
