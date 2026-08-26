# GitHub project and delivery setup

This guide replaces the previous .NET/AWS-specific repository setup. The branch and review model remains useful; deployment automation is intentionally paused until the TypeScript hosting architecture is selected.

## Branch lifecycle

The normal integration path is:

```text
feature/* or refactor/* -> development -> release/* -> master
```

- `development` is the integration branch.
- `master` is the production-history branch.
- ordinary feature/refactor work should target `development`, not `master`.
- release automation should not be reconstructed until the new deployment target exists.

## Development ruleset

Protect `development` with:

- pull requests required;
- CI required;
- conversation resolution required;
- force pushes disabled;
- branch deletion disabled;
- one independent approval when another reviewer is available.

The current CI contract is:

```bash
pnpm db:generate
pnpm typecheck
pnpm test
pnpm build
```

Choose the exact GitHub check name shown by the first successful run when configuring required status checks rather than guessing it in advance.

## Master ruleset

Protect `master` with:

- pull requests required;
- CI required;
- conversation resolution required;
- force pushes disabled;
- branch deletion disabled;
- independent human approval before production merge when available.

Do not merge ordinary feature branches directly to `master`.

## Codex review

Codex review remains separate from CI. The repository keeps `.github/workflows/codex-review-request.yml`, which targets pull requests into `development` and requests one idempotent review/description update.

Use the `HouseKeeper PR Review` environment described in [`codex-environment.md`](codex-environment.md). Treat findings as review evidence, not as a synthetic required check.

## GitHub Actions permissions

Keep default workflow permissions read-only. CI requires repository-content read access only. The Codex request workflow requires contents read and pull-request write access; it must not check out or execute untrusted pull-request code under `pull_request_target`.

Future deployment workflows should use short-lived OIDC identity rather than long-lived cloud keys.

## Environments and deployment

The retired C# CDK/ECS/CloudFront deployment path is no longer authoritative for this branch. Do not carry its environment variables, certificates, roles, or smoke-token assumptions into the TypeScript deployment by default.

When hosting is selected, create a separate ADR that answers:

- where Next.js executes and how server rendering/API routes are hosted;
- where PostgreSQL is hosted and how network access is constrained;
- migration execution and rollback strategy;
- authentication provider/session model;
- secrets management and OIDC trust;
- preview/shared-development/production environment topology;
- observability, backups, recovery objectives, and cost ceilings.

Only then should protected deployment environments and release promotion workflows be reintroduced.

## Pull-request completeness

A PR is complete when applicable implementation, tests, schema/migration notes, architecture documentation, operational implications, and known gaps are all represented. The pull-request template is tracker-neutral and should link the relevant GitHub/Notion/etc. work item when one exists.
