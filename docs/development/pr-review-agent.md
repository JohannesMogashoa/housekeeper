# Codex pull-request review contract

Codex is a technical reviewer for HouseKeeper, not a merge authority. GitHub CI and human disposition remain independent evidence.

## Priority order

Review findings in this order:

1. authorization and household-isolation defects;
2. data loss, schema/migration, transaction, or concurrency defects;
3. server/client boundary leaks and secret exposure;
4. incorrect tRPC contracts or validation behavior;
5. Next.js rendering/caching correctness;
6. accessibility and user-visible regressions;
7. maintainability issues that materially increase change risk.

Do not report formatting preferences that are already mechanically enforced or speculative abstractions without a concrete failure mode.

## Repository boundaries

- `apps/web` owns Next.js transport/presentation and app-local shadcn components.
- `packages/api` owns tRPC procedures, validation, authorization, and orchestration.
- `packages/db` owns PostgreSQL schema and access.
- React components must not query PostgreSQL directly.
- Household-scoped operations must prove membership server-side.

## Evidence

Prefer executable evidence when available:

```bash
pnpm typecheck
pnpm test
pnpm build
```

For database changes also inspect Drizzle-generated migration SQL. A generated migration being syntactically valid does not prove it is safe for existing data.

## Finding format

Each material finding should identify:

- severity and concrete impact;
- exact file/behavior involved;
- a reproducible or logically complete failure scenario;
- the smallest credible correction.

If there are no material findings, say so without inventing low-value comments.
