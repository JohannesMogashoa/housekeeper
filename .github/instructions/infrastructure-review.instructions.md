---
applyTo: "deploy/**,.github/workflows/**,scripts/**,build/**,Dockerfile*,**/*.bicep,**/*.bicepparam,**/compose*.yml,**/compose*.yaml"
---

# Infrastructure and delivery review instructions

When reviewing matching files, verify the following in addition to repository-wide instructions.

## Identity and secrets

- GitHub-to-Azure authentication uses OpenID Connect federation with narrow repository, branch/environment, and audience trust.
- Application access uses managed identity wherever supported.
- Long-lived service-principal secrets, publish profiles, storage account keys, and committed credentials are not the default deployment path.
- Workflow permissions are explicitly minimized; pull-request jobs have no production/deployment credentials.
- Secrets are not printed, uploaded as artifacts, emitted as Bicep outputs, or passed to untrusted pull-request code.

## Bicep and environments

- Resource location, naming, tags, environment isolation, ownership, retention, budgets, and role assignments are explicit.
- Changes run Bicep build/lint and `what-if` before protected apply.
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

- The SDK/tool/dependency graph is pinned or intentionally serviced and remains vulnerability-audited.
- API and PWA are built once; the exact immutable artifacts are promoted.
- Published PWA output is checked for unresolved static-asset placeholders.
- Post-deployment readiness, authenticated smoke, browser, and persistence checks are appropriate to the change.
- Workflow concurrency, cancellation, environment protection, timeouts, artifact retention, and failure diagnostics are deliberate.

## Local and CI parity

- Local scripts remain available for Bash and PowerShell where the workflow is part of the supported cross-platform contract.
- Docker Compose/Testcontainers infrastructure is disposable or has explicit persistent-volume/reset behavior.
- CI does not depend on a shared long-lived database or production cloud access.
- Health/readiness checks reflect real dependencies without exposing sensitive internals.

## Operations and cost

- Telemetry, alerts, backup/restore, soft-delete, rollback, and runbook impact are updated for operational changes.
- Cost-significant resources and Defender/telemetry ingestion changes are called out.
- Destructive, irreversible, or downtime-bearing changes include a safe rollout and recovery plan.

Treat excessive permissions, secret exposure, implicit migrations, missing `what-if`, artifact rebuilding, unsafe rollback, or absent environment isolation as material findings.