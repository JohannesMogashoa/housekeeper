# Codex instructions for local and CI scripts

These instructions apply to `scripts/` in addition to the repository root `AGENTS.md`.

Scripts are part of HouseKeeper's delivery and verification surface. Preserve Bash and PowerShell parity when a workflow is supported on both platforms.

## Orchestration and isolation

- Docker Compose and Testcontainers dependencies are disposable by default or have explicit persistence and reset behavior.
- Local and CI scripts must not depend on a shared long-lived database, production cloud access, or developer-specific machine state.
- Production credentials, storage keys, publish profiles, database credentials, and other secrets must not be embedded, echoed, uploaded, or passed to untrusted pull-request code.
- Pin or deliberately service SDKs, tools, container images, and dependency inputs used by scripts.

## Health, smoke, and persistence checks

- Health and readiness checks reflect real required dependencies without exposing sensitive internals.
- Smoke checks fail clearly, use bounded timeouts, preserve useful diagnostics, and do not report success before the system is genuinely ready.
- Persistence checks make reset/reuse semantics explicit and prove the intended restart behavior rather than relying on process-local state.
- Script failures propagate a non-zero exit code; cleanup is cancellation-safe and does not hide the original failure.

Treat production access, secret exposure, shared mutable infrastructure, misleading readiness, unbounded waits, platform drift, or false-positive verification as material review findings.
