# Local Development

## Prerequisites

- Node.js 24+
- pnpm 11+
- Docker with Compose

## Bootstrap

```bash
pnpm install
pnpm infra:up
cp apps/web/.env.example apps/web/.env.local
pnpm db:push
pnpm dev
```

PostgreSQL is exposed on `localhost:5432` with local-only credentials. Next.js reads `apps/web/.env.local`; a root `.env` is intentionally not used because Turbo executes the web task from the application workspace.

## Workspace commands

Run commands from the repository root so Turborepo can order package work correctly:

```bash
pnpm dev
pnpm typecheck
pnpm test
pnpm build
```

Target a package when investigating a local failure:

```bash
pnpm --filter @housekeeper/api test
pnpm --filter @housekeeper/db typecheck
pnpm --filter @housekeeper/web build
```

## Database

```bash
pnpm db:generate
pnpm db:push
pnpm db:studio
```

`db:push` is a local restructuring convenience only. Do not use it as a production deployment mechanism.

The PostgreSQL 18 official image uses `/var/lib/postgresql` as its volume target; do not change the Compose mount back to `/var/lib/postgresql/data` unless the image major version is also changed and the migration implications are understood.

Stop local infrastructure with:

```bash
pnpm infra:down
```

## Development identity

The tRPC route creates a deterministic `development-user` identity only when `NODE_ENV !== "production"`. This keeps authorization/membership logic active during local development without coupling feature procedures to an authentication vendor.

Production requests are unauthorized until a real session adapter is implemented.
