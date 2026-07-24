# Foundation Phase Backlog

This backlog converts the Discovery 0 decisions into executable, testable slices. The canonical task IDs below are assigned by the HouseKeeper Notion backlog and define the intended execution order and dependency model.

## Planning rules

- Each item must leave the repository releasable.
- Infrastructure work must prove an application journey, not stop at resource creation.
- Security controls require negative-path tests.
- Cross-module behaviour requires a named consistency and retry model.
- Production cloud access is introduced only through managed identity or GitHub OIDC.
- A task is complete only when evidence is linked from its Notion page and CI is green.

## Execution waves

```text
Wave 1 — close discovery validation
  HK-16 Physical-device PWA validation

Wave 2 — production trust and delivery boundary
  HK-17 Entra External ID integration
  HK-18 Azure development environment and OIDC pipeline

Wave 3 — household and command foundations
  HK-19 Household invitations and membership roles
  HK-20 Idempotent command and offline replay protocol

Wave 4 — durable application plumbing
  HK-21 Transactional outbox, inbox and worker foundation
  HK-22 Client offline pending-action queue

Wave 5 — first business and supporting modules
  HK-23 Tasks and Routines foundation slice
  HK-24 Attachment storage vertical slice
  HK-25 Notifications and Web Push vertical slice

Wave 6 — operational readiness
  HK-26 Observability, backup and recovery baseline
  HK-27 Foundation release gate
```

## HK-16 — Validate the PWA on physical mobile devices

**Priority:** P0  
**Estimate:** S  
**Dependencies:** HK-15 technical recommendation

### Outcome

The approved PWA-first decision has recorded evidence on representative Android and iOS devices rather than only desktop Chromium automation.

### Acceptance criteria

- The published release build opens in a physical Android browser.
- The app is installable and launches from the home screen.
- iOS installation guidance is verified on a physical iPhone or iPad where available.
- The app shell opens after network loss.
- Browser storage survives app closure and relaunch.
- Camera/file selection is exercised with a temporary test page or attachment spike.
- Mobile viewport has no horizontal overflow at the agreed minimum width.
- Compressed initial payload, first launch and repeat launch observations are recorded on a representative South African mobile connection.
- Browser-specific limitations are added to the technical recommendation.

## HK-17 — Integrate Microsoft Entra External ID

**Priority:** P0  
**Estimate:** L  
**Dependencies:** HK-15; existing development authentication boundary

### Outcome

A real external user can sign in through Entra External ID, the API validates the token, and the subject maps to an application member without trusting provider roles for household authorization.

### Acceptance criteria

- Separate local, CI and shared-development authentication modes are documented.
- The PWA completes authorization-code flow with PKCE using supported Microsoft libraries.
- The API validates issuer, audience, lifetime and signing keys.
- External subject mapping is unique and immutable.
- Development authentication cannot activate outside `Development`.
- Household access still resolves from application-owned membership rows.
- Invalid issuer, invalid audience, expired token, missing subject and cross-household access tests pass.
- No client secret, storage credential or privileged token enters the PWA.

## HK-18 — Provision the shared Azure development environment

**Priority:** P0  
**Estimate:** L  
**Dependencies:** HK-15 hosting decision

### Outcome

A reproducible shared development environment is provisioned in South Africa North and deployed from GitHub without long-lived Azure credentials.

### Acceptance criteria

- Bicep modules cover Static Web Apps, Linux App Service, PostgreSQL Flexible Server, Storage, Key Vault, monitoring, budgets and role assignments as applicable.
- GitHub authenticates through OIDC federation.
- App Service uses managed identity for supported Azure access.
- Development resources use synthetic/disposable data and an isolated resource group.
- Bicep validation and `what-if` run in pull requests.
- A protected workflow applies infrastructure, migrations, API and PWA artifacts.
- Readiness and authenticated smoke tests pass after deployment.
- Resource names, tags, retention, budgets and teardown guidance are documented.

## HK-19 — Implement household invitations and membership roles

**Priority:** P0  
**Estimate:** L  
**Dependencies:** HK-17

### Outcome

A household owner can invite another authenticated person, and the invited member can join with an application-owned role.

### Acceptance criteria

- Owner, Member and any explicitly approved MVP roles are represented in Households domain state.
- Invitation tokens are random, time-limited, single-use and stored safely.
- Accepting an invitation is idempotent.
- Removing a member revokes access without deleting historical references.
- Owner-only operations use a reusable authorization contract.
- Cross-household, expired, replayed, removed-member and last-owner safety tests pass.
- Audit evidence records invitation and membership lifecycle changes without storing secrets.

## HK-20 — Define the idempotent command and offline replay protocol

**Priority:** P0  
**Estimate:** M  
**Dependencies:** HK-17 and HK-19

### Outcome

Mutation APIs have a consistent retry contract suitable for unreliable connectivity and a browser-side pending-action queue.

### Acceptance criteria

- Every replayable command carries a client-generated operation identifier.
- Duplicate delivery returns the original logical outcome without duplicating side effects.
- Conflict responses distinguish retryable concurrency, validation, authorization and terminal state changes.
- Problem Details responses use stable machine-readable reason codes.
- Operation records have bounded retention and household/member scope.
- API versioning and compatibility rules are documented.
- Integration tests cover response loss followed by replay, concurrent duplicate requests and cross-member operation-key reuse.

## HK-21 — Implement transactional outbox, inbox and worker foundation

**Priority:** P0  
**Estimate:** L  
**Dependencies:** HK-20 and the PostgreSQL module-boundary decision

### Outcome

A committed module change can produce a durable integration event, and another module can consume it idempotently after process restart.

### Acceptance criteria

- Each event-producing module writes outbox state in the same transaction as its aggregate change.
- A leased worker claims rows using PostgreSQL coordination and bounded batches.
- Consumers persist inbox/deduplication evidence.
- Retry classification, backoff, terminal failure and poison-message handling are explicit.
- Event payloads are versioned and do not expose domain or EF entities.
- Queue age, retries, terminal failures and lease recovery are observable.
- Tests prove crash-after-commit recovery, duplicate delivery, handler failure, expired lease and API restart.
- The implementation can move to a separate host without changing module domain logic.

## HK-22 — Implement the client offline pending-action queue

**Priority:** P0  
**Estimate:** L  
**Dependencies:** HK-20

### Outcome

The PWA can retain an approved mutation offline and replay it safely after connectivity returns.

### Acceptance criteria

- Pending actions persist in IndexedDB or an equivalently durable browser store.
- Each action uses the HK-20 operation identifier and a versioned payload.
- Replay respects authentication state, household scope and ordering requirements.
- The UI distinguishes pending, retrying, conflicted and terminal actions.
- Backoff and manual retry behaviour are defined.
- Sign-out and household switching do not leak queued data across users.
- Playwright tests cover offline creation, browser reload, reconnect, duplicate replay and conflict presentation.
- Service-worker asset caching remains separate from business-data synchronization.

## HK-23 — Build the Tasks and Routines foundation slice

**Priority:** P0  
**Estimate:** XL  
**Dependencies:** HK-19, HK-20 and HK-21

### Outcome

A household member can define a simple recurring routine, materialize occurrences, view due work, and complete an occurrence while preserving history.

### Acceptance criteria

- The Tasks module owns the `work` schema and migration history.
- Versioned schedule definitions and materialized occurrences follow HK-8.
- Household, member and room references use scalar IDs and Households contracts.
- Date-only and timed schedules preserve IANA timezone snapshots.
- Completion and postponement are idempotent and append history.
- Schedule edits do not rewrite completed occurrence history.
- Domain, PostgreSQL integration, architecture, API, bUnit and Playwright tests cover the slice.
- Task events are published through HK-21.

## HK-24 — Build the attachment storage vertical slice

**Priority:** P1  
**Estimate:** XL  
**Dependencies:** HK-18, HK-19 and HK-21

### Outcome

An authorized household member can upload an approved image or PDF directly to Azure Blob Storage, pass validation/scanning, and link the ready attachment to a test owner record.

### Acceptance criteria

- The Attachments module owns its PostgreSQL schema and lifecycle state.
- Production uses managed identity and user-delegation SAS; shared account keys are not the application path.
- Upload grants are exact-object, short-lived and cannot list or overwrite arbitrary blobs.
- Size, media type, file signature, household quota and ownership checks are server-enforced.
- Malware scan results enter an idempotent inbox before an attachment becomes `Ready`.
- Rejected, abandoned and delete-pending objects are durably cleaned up.
- Azurite supports the local path; focused Azure smoke tests prove SAS, CORS and event integration.
- Cross-household upload, download and linking attempts are denied and tested.

## HK-25 — Build Notifications and Web Push vertical slice

**Priority:** P1  
**Estimate:** XL  
**Dependencies:** HK-18, HK-21 and HK-23

### Outcome

A due task occurrence creates an in-app reminder and, when the member opted in, attempts a Web Push notification through a durable dispatch record.

### Acceptance criteria

- Notifications owns its schema, preferences, subscriptions, dispatches and attempt history.
- Task events project reminder state idempotently.
- Date-only tasks schedule at 09:00 in the occurrence timezone snapshot.
- The dispatcher uses leases, bounded retries, suppression checks and terminal classification.
- Invalid subscriptions are retired without affecting other devices.
- The in-app notification remains available when push is disabled or fails.
- Push secrets and endpoints are never logged.
- Tests cover task completion before send, reassignment, duplicate events, expired lease, provider timeout, invalid subscription and API restart.

## HK-26 — Establish observability, backup and recovery baseline

**Priority:** P0  
**Estimate:** L  
**Dependencies:** HK-18 and HK-21

### Outcome

The shared environment exposes actionable telemetry and has tested deployment, rollback and data-recovery procedures.

### Acceptance criteria

- OpenTelemetry-compatible traces, metrics and structured logs export to Azure Monitor/Application Insights.
- Trace context spans PWA requests, API handling, database operations and background dispatch where practical.
- Dashboards cover API health, latency, failures, PostgreSQL saturation, queue age, retries, storage/scanning failures and authorization denials.
- Alerts have an owner, severity, threshold rationale and response note.
- A PostgreSQL restore drill is executed into an isolated target and records observed recovery point/time.
- Blob soft-delete and reconciliation behaviour are tested.
- Deployment rollback restores the prior application artifact without attempting unsafe down-migrations.
- Runbooks cover deployment failure, migration failure, database restore and background-queue backlog.

## HK-27 — Execute the Foundation release gate

**Priority:** P0  
**Estimate:** M  
**Dependencies:** HK-16 through HK-26, with P1 capabilities included only when required for the agreed Foundation exit

### Outcome

The architecture baseline is proven in a production-like environment and the repository is ready for sustained MVP feature delivery.

### Acceptance criteria

- All P0 Foundation tasks are Done and linked to evidence.
- The complete solution restores, builds, tests and publishes from a clean runner.
- A new environment can be provisioned from Bicep and configured without undocumented manual changes.
- Production-style authentication, household authorization, offline replay and durable event delivery pass end-to-end.
- The first Tasks slice survives browser reload, API restart and redeployment.
- Security scanning and negative authorization tests are green.
- Performance observations cover PWA payload, API latency and database/worker contention at the agreed private-beta load.
- Operational runbooks and ownership are reviewed.
- Deferred decisions and revisit triggers are still accurate.
- The next MVP backlog is reordered using Foundation evidence rather than Discovery assumptions.
