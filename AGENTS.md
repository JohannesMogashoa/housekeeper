# HouseKeeper Codex operating and review guide

This file is the root instruction contract for OpenAI Codex in this repository. It applies to Codex cloud tasks, GitHub pull-request reviews, CLI/IDE sessions, and any delegated implementation work.

More specific `AGENTS.md` files deeper in the repository add rules for their directory trees. Apply the root rules and every applicable nested file.

## Start with the work item

Before changing or reviewing code:

1. Read the linked HK GitHub issue completely.
2. Identify the required outcome, dependencies, exclusions, acceptance criteria, and completion evidence.
3. Read `docs/architecture/technical-recommendation.md` and `docs/architecture/adr/README.md`.
4. Inspect the complete diff and enough surrounding implementation to understand behavior.
5. Do not silently supersede an accepted ADR. Propose and justify a new decision.

## Architecture invariants

- HouseKeeper remains a .NET 10 modular monolith with a standalone Blazor WebAssembly PWA, an ASP.NET Core API composition host, and PostgreSQL.
- `HouseKeeper.Api` owns composition, middleware, endpoint mapping, authentication plumbing, and transport adaptation. It does not own domain rules or module persistence.
- Each `HouseKeeper.Modules.*` assembly owns one business capability and must not reference another module implementation, entity, repository, infrastructure namespace, table, or `DbContext`.
- Cross-module synchronous behavior uses purpose-specific contracts. Cross-module reactions use immutable, versioned integration events.
- Each persistent module owns its PostgreSQL schema, EF Core context, migrations, indexes, constraints, and transaction boundary.
- `HouseKeeper.Contracts` contains dependency-light messages only. Do not expose aggregates, EF types, `IQueryable`, provider SDK types, or mutable shared business objects.
- Shared building blocks require proven identical technical semantics across at least two modules. Do not create generic repositories or speculative abstractions.

## Security and household isolation

- Treat the PWA, browser storage, request payloads, provider callbacks, files, and all client claims as untrusted.
- Authentication proves identity. Application-owned household membership and role state determines authorization.
- Protected operations are default-deny and verify household scope before reading or mutating business data.
- Add negative tests for anonymous, non-member, wrong-role, removed-member, and cross-household attempts when relevant.
- Keep access tokens, authorization codes, invitation tokens, SAS URLs, push endpoints/secrets, signing keys, database credentials, file contents, and unnecessary personal data out of logs, telemetry, errors, artifacts, and client assets.

## Data, migrations, and reliability

- Apply migrations explicitly through developer or protected deployment workflows. The API must not mutate production schemas during normal startup.
- Cross-module references are scalar IDs without ORM navigation properties or cross-schema cascades.
- Preserve historical meaning using versioned definitions or append-only evidence where required.
- Replayable mutations use the approved operation-ID and idempotency protocol.
- Durable cross-module events use transactional outbox and idempotent inbox semantics.
- Workers use bounded batches, leases, cancellation, retry classification, backoff, terminal failure handling, observability, and restart recovery.
- Never claim exactly-once external delivery.
- Review response loss, duplicate delivery, concurrent execution, process termination, expired leases, stale membership, timezone behavior, and provider failure where relevant.

## Change discipline

- Make the smallest coherent change that fully satisfies the issue.
- Do not add unrelated refactors, premature services, hidden scope expansion, or broad suppressions.
- Keep package versions central, dependencies pinned or deliberately serviced, and vulnerability auditing enabled.
- Update code-adjacent documentation, diagrams, migrations, scripts, runbooks, and operational notes when behavior changes.
- Preserve Bash and PowerShell parity for supported cross-platform workflows.

## Verification

Use the smallest complete test portfolio for the changed risk:

- xUnit v3 on Microsoft Testing Platform v2 for domain, application, and integration behavior;
- real PostgreSQL for persistence, migration, transaction, locking, and concurrency semantics;
- architecture tests for assembly and dependency boundaries;
- API and authorization tests for protected operations;
- bUnit for deterministic Razor component behavior;
- Playwright against published artifacts for critical browser journeys;
- failure-injection and restart tests for idempotency, outbox/inbox, workers, and provider failures;
- CDK strict synth, policy/security checks, reviewed diff, deployment smoke, rollback, and recovery evidence for infrastructure changes.

A green pipeline is supporting evidence, not proof of correctness.

## Pull-request review contract

When Codex reviews a pull request, prioritize correctness over style in this order:

1. cross-household isolation, authorization, and secret safety;
2. data integrity, migration safety, concurrency, idempotency, and restart recovery;
3. modular-monolith dependencies, schema ownership, and contract boundaries;
4. PWA/offline correctness, browser storage isolation, accessibility, and service-worker safety;
5. external-provider failure handling and durable worker behavior;
6. test adequacy, observability, deployment safety, rollback, and documentation drift;
7. maintainability only when there is a concrete failure or change-cost impact.

Report only actionable findings supported by changed code, surrounding implementation, or missing required evidence.

Use Codex's native priority labels:

- **[P0]** — stop-ship defect causing a credible security boundary breach, cross-household exposure, secret exposure, data loss/corruption, unsafe migration, or repository-wide failure.
- **[P1]** — urgent defect likely to cause production failure, missing authorization, a broken invariant, non-idempotent side effect, concurrency/restart failure, or absent critical test.
- **[P2]** — material correctness, reliability, observability, accessibility, compatibility, or maintainability defect that should normally be resolved before merge.
- **[P3]** — narrowly scoped, non-urgent improvement with concrete value; never use this priority for personal preference or optional style.

For every finding include:

1. priority;
2. file and line/range;
3. violated invariant or issue requirement;
4. realistic failure mode;
5. smallest safe correction;
6. test or evidence required to prove the correction.

Separate defects from questions and non-blocking suggestions. Avoid vague "consider" comments, praise-only noise, speculative abstractions, and unrelated refactors.

When no material finding exists, state that clearly, summarize the evidence reviewed, and list residual manual validation gaps.

## Codex GitHub review usage

- Request an on-demand review by commenting `@codex review` on the pull request.
- Add targeted focus when useful, for example `@codex review for household authorization, migration safety, and restart recovery`.
- Re-run review after material changes or use Codex automatic repository reviews when enabled.
- Codex review supplements the full CI pipeline and deliberate human approval; it replaces neither.


# AWS Guidance

- Prefer the AWS MCP Server for AWS interactions — it provides sandboxed
  execution, observability, and audit logging. If unavailable, use the
  AWS CLI directly.
- Before starting a task, check whether a relevant AWS skill is available.
  Load the skill with `retrieve_skill` and prefer its guidance over
  general knowledge.
- When uncertain about specific AWS details (API parameters, permissions,
  limits, error codes), verify against documentation rather than guessing.
  State uncertainty explicitly if you cannot confirm.
- When creating infrastructure, prefer infrastructure-as-code (AWS CDK or
  CloudFormation) over direct CLI commands.
- When working with infrastructure, follow AWS Well-Architected Framework
  principles.
- Do not use em dashes in AWS resource names or descriptions. Use
  hyphens instead.

## Secret Safety

- MUST load the `aws-secrets-manager` skill first for any secret,
  credential, API key, token, or password task. MUST NOT call
  `secretsmanager get-secret-value` or `batch-get-secret-value`, and MUST
  NOT hit the Secrets Manager Agent daemon directly. MUST use
  `{{resolve:secretsmanager:secret-id:SecretString:json-key}}` with
  `asm-exec` so the secret resolves at runtime without entering context.
