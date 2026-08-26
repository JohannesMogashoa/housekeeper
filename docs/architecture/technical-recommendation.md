# Technical Recommendation: TypeScript Modular Monolith

## Decision

HouseKeeper should operate as a TypeScript modular monolith in a pnpm/Turborepo workspace, with Next.js as the web/runtime host, tRPC as the application boundary, PostgreSQL as the system of record, and Drizzle as the persistence adapter.

This replaces the previous ASP.NET + Blazor application structure rather than wrapping it.

## Why this shape

The product is still early enough that independently deployable services would increase coordination cost without buying meaningful operational isolation. The useful separation is **code ownership and dependency direction**, not network boundaries.

```text
Browser
  │
  ▼
apps/web (Next.js)
  │  typed tRPC client
  ▼
packages/api
  │
  ├── Zod validation
  ├── authorization / membership checks
  └── feature orchestration
          │
          ▼
      packages/db
          │
          ▼
      PostgreSQL
```

### Dependency rules

1. `apps/web` may import public contracts/types from `packages/api`.
2. `packages/api` may import database primitives from `packages/db`.
3. `packages/db` must not import application or web code.
4. React components never access PostgreSQL directly.
5. Household authorization is enforced in server procedures, not inferred from UI state.

## Feature organization

Prefer vertical tRPC routers rather than generic repository/service layers created in advance:

```text
packages/api/src/
├── domain/
├── routers/
│   ├── households.ts
│   ├── tasks.ts
│   ├── maintenance.ts
│   └── shopping.ts
├── router.ts
└── trpc.ts
```

Extract shared domain modules only when at least two features need the same behavior. This keeps the model explicit without recreating framework-heavy layering.

## Persistence

PostgreSQL is the durable source of truth. Drizzle owns table/schema declarations and typed queries. Multi-write invariants use database transactions.

The Households port intentionally maps the already-deployed physical model rather than inventing a second schema: PostgreSQL schema `households`, the existing quoted EF column names, the composite household-member key, the existing subject index, `Owner` role value, and cascade foreign key are preserved.

That physical compatibility does **not** make EF migration history compatible with Drizzle migration history. Existing environments require a one-time baseline before Drizzle migrations become authoritative; see ADR-0002.

## API boundary

tRPC is an internal typed API boundary, not an excuse to collapse server and client concerns. Procedures own:

- authentication preconditions;
- authorization and household isolation;
- input validation;
- transactions/application orchestration;
- transport-safe output shapes.

Dates are emitted across the current tRPC boundary as ISO-8601 strings rather than relying on an implicit JSON `Date` representation.

If HouseKeeper later exposes a public or third-party API, add a REST/OpenAPI adapter over application capabilities rather than exposing internal tRPC semantics as the public contract.

## UI

Tailwind CSS 4 provides design primitives. shadcn/ui components remain copy-owned in `apps/web/components/ui` because there is one web application today. A separate UI workspace package should only be introduced when another application genuinely consumes the same components.

Next.js server components are the default rendering model. Client components are reserved for interaction-heavy surfaces and client tRPC hooks.

## Authentication

Authentication provider selection is intentionally decoupled from this migration. The tRPC context exposes a `userId` seam. Development uses a deterministic local identity; production refuses to synthesize an identity.

When authentication is selected, only the Next.js request-context adapter should translate the provider session/token into the application `userId`. Feature routers should not depend directly on provider SDKs.

## Deployment

The previous C# CDK deployment project was removed because retaining a second language solely for infrastructure would undermine the TypeScript migration and preserve assumptions tied to the retired runtime.

Do not reintroduce production deployment automation until the hosting model is chosen. Reasonable future targets include a Next.js-capable managed host plus managed PostgreSQL, or an AWS-native TypeScript CDK deployment. That decision should be a separate ADR with cost, operational burden, networking, migration, and observability implications.

## Quality gates

Every pull request should pass:

```bash
pnpm db:generate
pnpm typecheck
pnpm test
pnpm build
```

Integration tests that require PostgreSQL should be added once the next persistence-heavy feature is implemented; CI can then attach PostgreSQL 18 as a service container.
