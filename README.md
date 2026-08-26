# HouseKeeper

HouseKeeper is a household-management application focused on answering a simple question: **what does the home need today?**

This branch restructures the project as a TypeScript-first modular monolith built on Next.js, tRPC, PostgreSQL, Tailwind CSS, shadcn/ui conventions, and Turborepo.

## Technology stack

- **Runtime:** Node.js 24
- **Language:** TypeScript 7
- **Web:** Next.js 16 App Router + React 19
- **API:** tRPC 11
- **Database:** PostgreSQL 18
- **Data access:** Drizzle ORM
- **Validation:** Zod 4
- **Styling:** Tailwind CSS 4
- **UI:** shadcn/ui copy-owned components
- **Workspace:** pnpm 11 + Turborepo 2
- **Tests:** Vitest 4

## Repository structure

```text
housekeeper/
├── apps/
│   └── web/                 # Next.js application and shadcn UI
├── packages/
│   ├── api/                 # tRPC routers + application/domain boundaries
│   └── db/                  # Drizzle schema + PostgreSQL client
├── infra/
│   └── local/               # Docker Compose for local PostgreSQL
├── docs/
│   ├── architecture/
│   └── development/
├── turbo.json
└── pnpm-workspace.yaml
```

The structure deliberately avoids splitting the product into deployable services prematurely. tRPC gives the web application an end-to-end typed boundary while keeping feature logic independent of React. PostgreSQL remains behind `packages/db`, so a future worker, mobile API adapter, or background process can reuse the same persistence model without importing the web application.

## Local development

Requirements:

- Node.js 24+
- pnpm 11+
- Docker with Compose

Install dependencies and start PostgreSQL:

```bash
pnpm install
pnpm infra:up
```

Create the Next.js local environment file where Next.js will actually load it:

```bash
cp apps/web/.env.example apps/web/.env.local
```

Apply the current schema during this scaffold phase and start the workspace:

```bash
pnpm db:push
pnpm dev
```

The web application runs at `http://localhost:3000` by default.

### Authentication seam

The previous application already treated household access as authenticated and membership-scoped. The TypeScript port preserves that server-side boundary. During development, the Next.js tRPC route uses a fixed development identity. In production it deliberately resolves no user until a real authentication adapter is wired; it does **not** trust a client-supplied identity header in production.

## Current vertical slice

The migration ports the existing Households capability first because it is the only feature that already contained meaningful domain and persistence behavior:

- household names are trimmed and constrained to 2–120 characters;
- creating a household also creates its owner membership in one transaction;
- listing households is constrained by the current user's membership;
- PostgreSQL owns generated IDs and creation timestamps.

Placeholder .NET modules were not mechanically reproduced. Tasks, maintenance, shopping, notifications, and attachments should be added as real TypeScript vertical slices as their requirements are implemented.

## Database workflow

During the restructuring branch, `pnpm db:push` is optimized for local iteration. `pnpm db:generate` proves that Drizzle can derive migrations from the checked-in schema.

Before the first production deployment, generated migrations must be reviewed and committed, and deployment must execute migrations as a distinct release step. Production should never depend on `drizzle-kit push`.

Useful commands:

```bash
pnpm db:generate
pnpm db:push
pnpm db:studio
```

PostgreSQL 18 changed the official container's data-volume location. Local Compose therefore mounts the named volume at `/var/lib/postgresql`, not the pre-18 `/var/lib/postgresql/data` path.

## Validation

```bash
pnpm typecheck
pnpm test
pnpm build
```

GitHub CI performs schema generation, typechecking, tests, and a production build.

## Architectural direction

See [`docs/architecture/technical-recommendation.md`](docs/architecture/technical-recommendation.md) and ADR-0001 for the rationale and dependency rules behind the migration.
