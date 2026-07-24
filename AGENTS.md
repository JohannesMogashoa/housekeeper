# HouseKeeper agent operating guide

This file applies to every AI agent working in the repository. The dedicated pull-request reviewer is defined in `.github/agents/housekeeper-reviewer.agent.md`.

## Start with the work item

- Read the linked HK GitHub issue completely before changing or reviewing code.
- Preserve the requested outcome, dependencies, exclusions, acceptance criteria, and completion evidence.
- Read `docs/architecture/technical-recommendation.md` and `docs/architecture/adr/README.md` before making architectural decisions.
- Do not silently supersede an accepted ADR. Propose a new decision and explain consequences.

## Work within the architecture

- Keep the standalone PWA, API composition host, capability modules, contracts, and module-owned schemas distinct.
- Never bypass application-owned household authorization.
- Do not introduce cross-module implementation references, direct table access, or shared mutable domain entities.
- Keep migrations explicit and outside normal production API startup.
- Preserve idempotency, concurrency, restart recovery, and failure classification where the work item depends on them.

## Change discipline

- Make the smallest coherent change that fully satisfies the issue.
- Do not add speculative abstractions, generic repositories, premature services, unrelated refactors, or silent scope expansion.
- Keep package versions central and vulnerability auditing enabled.
- Keep logs, telemetry, errors, test artifacts, and browser storage free of secrets and sensitive grants.
- Update code-adjacent documentation, diagrams, scripts, runbooks, and migrations when behavior changes.

## Verification

- Run the risk-appropriate test layers and the standard warnings-as-errors build.
- Add negative authorization and cross-household tests for protected features.
- Add real PostgreSQL tests for persistence semantics.
- Add failure-injection/restart tests for idempotency, outbox/inbox, workers, or external providers.
- Add bUnit and published Playwright journeys for user-facing PWA behavior.
- Record exact commands, CI runs, residual gaps, and manual evidence in the pull request template.

## Review

- Use the `housekeeper-reviewer` custom agent for a dedicated review session.
- Treat AI review as supplemental evidence. Final merge requires deliberate human approval.
