## Linked work

- HK issue: <!-- Example: Closes #4 -->
- Notion task: <!-- Paste the canonical HK page URL -->
- Dependencies completed: <!-- List prerequisite issues/PRs or explain why none -->

## Outcome

<!-- Describe the user/business outcome now possible. Do not only list files changed. -->

## Implementation

<!-- Summarize domain behavior, API/UI flow, persistence, module interactions, and important trade-offs. -->

## Architecture and boundary review

- [ ] The change follows `docs/architecture/technical-recommendation.md` and the ADR index.
- [ ] The API remains a composition/transport host and contains no new domain rules.
- [ ] No module implementation references another module implementation, schema, entity, repository, or `DbContext`.
- [ ] Cross-module behavior uses purpose-specific contracts or versioned integration events.
- [ ] New shared abstractions are justified by proven multi-module semantics.
- [ ] Any deliberate architecture deviation is documented by a superseding/proposed ADR.

## Security and household isolation

- [ ] Protected operations are default-deny and verify household membership/role server-side.
- [ ] Anonymous, non-member, wrong-role, removed-member, and cross-household paths are tested where relevant.
- [ ] No secret, token, invitation token, SAS URL, push secret/endpoint, or sensitive payload enters client assets, logs, telemetry, or artifacts.
- [ ] Input validation, rate limiting, file policy, and provider authorization are addressed where relevant.

## Data, migrations, and reliability

- Database/schema impact: <!-- None, or describe owner module/schema/context/migration -->
- Migration/rollout order: <!-- Include expand-and-contract or downtime notes -->
- Rollback constraints: <!-- State whether previous app artifact remains compatible -->
- Idempotency/concurrency behavior: <!-- Include operation IDs, unique constraints, concurrency tokens -->
- Restart/retry behavior: <!-- Include outbox/inbox, leases, provider failure, browser replay as applicable -->

- [ ] Persistent state has one owning module and explicit migration/index/constraint changes.
- [ ] The API does not apply production migrations during normal startup.
- [ ] Replay, concurrency, response-loss, duplicate delivery, process restart, and stale authorization are tested where relevant.

## PWA and user experience

- [ ] Loading, empty, offline, pending, retrying, conflict, validation, authorization, and terminal-error states are handled where relevant.
- [ ] Browser-persisted data is isolated by authenticated user/household and contains no tokens.
- [ ] Accessibility, keyboard/focus behavior, touch targets, mobile viewport, and browser-specific behavior were reviewed.
- [ ] Service-worker changes do not cache private API responses or conflate asset caching with business-data synchronization.

Screenshots/recordings: <!-- Required for visible UI changes; include mobile viewport evidence -->

## Verification

### Automated

- [ ] Restore and warnings-as-errors build
- [ ] Domain/application tests
- [ ] Real PostgreSQL integration/migration tests
- [ ] API and authorization tests
- [ ] Architecture tests
- [ ] bUnit component tests
- [ ] Playwright published browser journey
- [ ] Failure-injection/restart/retry tests
- [ ] CDK build/strict synth/policy checks/reviewed diff and deployment smoke tests
- [ ] Coverage/diagnostic artifacts reviewed

Commands or workflow runs:

```text
Paste exact commands and GitHub Actions run links here.
```

### Manual

<!-- Device/browser/environment, scenario, result, and residual gaps. -->

## Operational impact

- Telemetry/metrics/logs:
- Alerts/runbooks:
- Backup/restore/retention:
- AWS cost/resource impact:
- Deployment and rollback evidence:

## Known risks and deferred work

<!-- State real residual risk. Link follow-up issues instead of hiding scope. -->

## Codex review request

- [ ] Commented `@codex review`, or confirmed automatic Codex review ran against the current PR head.
- [ ] Re-requested Codex review after material corrections where automatic review of new pushes is not enabled.
- Reviewer focus: <!-- Identify the highest-risk behavior; e.g. household isolation, migration safety, idempotency, restart recovery, offline state, or deployment permissions. -->
- Codex findings disposition: <!-- Link resolved threads or explain accepted residual risk. -->

## Human approval

- [ ] Material Codex findings are resolved or explicitly dispositioned.
- [ ] CI is green for the final PR head.
- [ ] A human reviewer has deliberately approved the change.
