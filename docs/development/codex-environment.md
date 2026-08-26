# HouseKeeper Codex review environment

This runbook defines the Codex cloud environment for HouseKeeper pull-request review and repository-aware implementation tasks after the TypeScript migration.

GitHub Actions remains the deterministic merge-evidence source. Codex exists to reproduce the smallest relevant subset of that evidence while investigating a change.

## Environment profile

| Setting | Value |
| --- | --- |
| Name | `HouseKeeper PR Review` |
| Repository | `JohannesMogashoa/housekeeper` |
| Normal review base | `development` |
| Node.js | `24` |
| pnpm | `11.23.0` |
| PostgreSQL | `18` |
| Setup script | `bash scripts/codex-setup.sh` |
| Maintenance script | `bash scripts/codex-maintenance.sh` |
| Agent internet access | `Off` |
| Secrets | None |

The setup phase may use network access to provision Node dependencies and PostgreSQL. Review-time agent internet access should remain disabled unless a specific investigation requires external evidence.

## Environment variable

Configure this ordinary disposable review variable:

```text
DATABASE_URL=postgres://housekeeper:housekeeper_codex@127.0.0.1:5432/housekeeper
```

Do not place production database credentials, authentication-provider secrets, AWS credentials, GitHub personal access tokens, or household data in the review environment.

## What setup does

`scripts/codex-setup.sh`:

1. activates Node.js 24;
2. activates pnpm 11.23.0 through Corepack;
3. installs PostgreSQL 18 from PGDG when required;
4. creates a disposable local role/database;
5. installs workspace dependencies while provisioning still has network access;
6. validates Drizzle schema generation and applies the schema to the disposable database;
7. runs TypeScript checks and unit tests.

The environment intentionally does not install the retired .NET SDK, AWS CDK CLI, PowerShell/Playwright .NET tooling, or AWS credentials.

## Cached environment maintenance

`scripts/codex-maintenance.sh` restarts PostgreSQL, reasserts the disposable database contract, restores cached pnpm dependencies in offline mode, and updates the local schema.

If a branch introduces dependencies that are not in the cached pnpm store, reset the Codex environment cache instead of enabling unrestricted review-time internet access.

## Review execution strategy

Read the root and applicable nested `AGENTS.md` files before selecting commands. Run the smallest complete validation portfolio that covers the changed risk; CI remains responsible for the whole pipeline.

### Baseline application change

```bash
pnpm typecheck
pnpm test
```

Run the production build when the change affects Next.js composition, routing, bundling, workspace dependencies, or server/client boundaries:

```bash
pnpm build
```

### Database or persistence change

```bash
pnpm db:generate
pnpm db:push
```

Review generated SQL before production use. `db:push` is allowed only against this disposable review database; production must use reviewed migrations.

Pay particular attention to constraints, indexes, transaction boundaries, cardinality, data compatibility, and household isolation.

### tRPC or authorization change

Review both the procedure middleware and every database predicate. Add targeted tests for anonymous access, non-members, removed members, wrong roles, and cross-household identifiers whenever the changed behavior requires them.

A successful TypeScript compile is not evidence of authorization correctness.

### UI change

Inspect:

- server-component versus client-component boundaries;
- keyboard and semantic accessibility;
- mobile layout;
- loading/error/empty states;
- mutation invalidation and stale-data behavior;
- whether sensitive server code is accidentally imported into a client bundle.

Browser automation can be added later when a critical user journey warrants Playwright in the TypeScript workspace.

## Security boundary

The Codex environment is a review sandbox, not a deployment workstation. It must never contain shared-development or production credentials.

Deployment review is static until a new TypeScript hosting target and deployment architecture are accepted in an ADR. Protected GitHub environments and short-lived provider identity should remain the future deployment boundary.

## GitHub review wiring

The existing `.github/workflows/codex-review-request.yml` remains deliberately separate from CI. For reviews targeting `development`, it creates one idempotent `@codex review` request and asks Codex to generate the PR description between the repository markers.

Material findings should be resolved or deliberately dispositioned before human approval; Codex review is evidence, not an artificial passing status.

## Verification

After creating or resetting the environment, confirm:

```bash
node --version
pnpm --version
psql --version
pg_isready --host 127.0.0.1 --port 5432
pnpm typecheck
pnpm test
```

Then open a small pull request to `development` and request `@codex review`.
