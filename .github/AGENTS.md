# Codex instructions for GitHub configuration

These instructions apply to `.github/` in addition to the repository root `AGENTS.md`.

## Workflow permissions and trust

- Declare workflow permissions explicitly and keep them at the minimum required scope.
- Pull-request workflows must not receive production credentials, deployment roles, publish profiles, or writable tokens unless a reviewed trusted-event design requires them.
- Do not execute untrusted fork or pull-request code in a privileged workflow context.
- Prefer GitHub OIDC for AWS authentication; avoid long-lived access keys.
- Pin third-party actions to immutable commit SHAs where practical and review supply-chain impact when updating them.

## Build and validation

- Preserve restore, vulnerability audit, warnings-as-errors build, test, publish, migration, smoke, Playwright, restart, persistence, and artifact evidence unless a documented change deliberately replaces a stage.
- Build API and PWA artifacts once and promote exact outputs; do not rebuild differently for deployment.
- Use concurrency/cancellation deliberately so obsolete runs stop without corrupting shared state.
- Set realistic timeouts and upload useful diagnostics even after failure, without leaking secrets.
- Keep service containers and test databases disposable and isolated per run.

## Deployment workflows

- Protected environment deployment requires deliberate approval where defined.
- Infrastructure changes run C# CDK build, strict synth, policy/security checks and reviewed diff before apply.
- Migrations use a protected identity separate from runtime access and remain explicit.
- Post-deployment readiness and authenticated smoke checks must fail the workflow when the environment is unhealthy.
- Rollback restores prior application artifacts and never assumes destructive down-migrations are safe.

## Pull-request evidence

- Preserve `.github/pull_request_template.md` sections that expose linked work, architecture, authorization, migrations, reliability, PWA behavior, tests, operations, risks, and reviewer focus.
- Authors request Codex review with `@codex review` or rely on an intentionally enabled automatic Codex review configuration.
- Do not add GitHub Copilot-specific review manifests or instructions unless a future explicit decision reverses the Codex selection.

Treat privilege escalation, secret exposure, unsafe event choice, unpinned high-risk actions, skipped validation, silent artifact rebuilding, or deployment without rollback evidence as material findings.
