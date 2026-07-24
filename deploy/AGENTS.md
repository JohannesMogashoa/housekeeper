# Codex instructions for deployment and infrastructure

These instructions apply to `deploy/` in addition to the repository root `AGENTS.md`.

## Identity and secrets

- GitHub-to-Azure authentication uses OpenID Connect federation with narrow repository, branch/environment, and audience trust.
- Application access uses managed identity wherever supported.
- Long-lived service-principal secrets, publish profiles, storage account keys, and committed credentials are not the default path.
- Secrets are not printed, uploaded as artifacts, emitted as Bicep outputs, or passed to untrusted pull-request code.

## Bicep and environments

- Resource location, naming, tags, environment isolation, ownership, retention, budgets, and role assignments are explicit.
- Infrastructure changes run Bicep build/lint and `what-if` before protected apply.
- Modules expose narrow typed parameters/outputs and avoid hidden portal-only dependencies.
- Repeated deployment is idempotent and teardown guidance is maintained for disposable environments.
- South Africa North remains the initial regional placement unless a documented decision supersedes it.

## Database and migrations

- Runtime and migration identities have separate privileges.
- Schema changes are explicit, reviewed, and applied outside normal API startup.
- Deployment order supports backward-compatible expand-and-contract behavior.
- Migration artifacts/manifests and diagnostic logs are retained without credentials.
- Rollback restores previous application artifacts and does not automatically run unsafe down-migrations.

## Build and deployment

- The SDK, tool, and dependency graph is pinned or deliberately serviced and remains vulnerability-audited.
- API and PWA are built once and the exact immutable artifacts are promoted.
- Published PWA output is checked for unresolved static-asset placeholders.
- Post-deployment readiness, authenticated smoke, browser, and persistence checks match the risk introduced.
- Workflow concurrency, cancellation, environment protection, timeouts, artifact retention, and failure diagnostics are deliberate.

## Local and CI parity

- Docker Compose and Testcontainers infrastructure is disposable or has explicit persistence/reset behavior.
- CI does not depend on a shared long-lived database or production cloud access.
- Health/readiness checks reflect real dependencies without exposing sensitive internals.

## Operations and cost

- Update telemetry, alerts, backup/restore, soft-delete, rollback, and runbook behavior for operational changes.
- Call out cost-significant resources and Defender or telemetry-ingestion changes.
- Destructive, irreversible, or downtime-bearing changes require a safe rollout and recovery plan.

Treat excessive permissions, secret exposure, implicit migrations, missing `what-if`, artifact rebuilding, unsafe rollback, or absent environment isolation as material review findings.
