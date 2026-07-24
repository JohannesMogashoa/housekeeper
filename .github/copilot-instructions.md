# HouseKeeper repository instructions

HouseKeeper is a .NET 10 mobile-first household management product implemented as a standalone Blazor WebAssembly PWA, an ASP.NET Core API, and a PostgreSQL-backed modular monolith.

Before proposing, implementing, or reviewing a change, read:
- `docs/architecture/technical-recommendation.md`;
- `docs/architecture/adr/README.md`;
- the linked HK GitHub issue;
- the nearest applicable `AGENTS.md` and `.github/instructions/*.instructions.md` files.

## Architecture

- Keep one backend deployable until a superseding ADR is accepted.
- `HouseKeeper.Api` is the composition root and transport host. Do not put domain rules, repositories, module entities, or module migrations in the API.
- Each `HouseKeeper.Modules.*` assembly owns one business capability and must not reference another module implementation.
- Cross-module synchronous access uses purpose-specific contracts. Cross-module reactions use versioned immutable integration events.
- Each persistent module owns its PostgreSQL schema, `DbContext`, migration history, indexes, and transaction boundary.
- `HouseKeeper.Contracts` contains dependency-light API/module messages only. Never expose EF entities, domain aggregates, `IQueryable`, provider SDK types, or mutable shared business objects.
- Add types to shared building blocks only when at least two modules need identical dependency-light technical semantics.

## Security and authorization

- Treat the PWA and all client input as untrusted.
- Keep secrets, database credentials, signing keys, storage credentials, and privileged provider tokens out of the PWA and repository.
- Authentication proves identity. Application-owned household membership and role state controls authorization.
- Protected operations are default-deny and verify household scope before reading or mutating business state.
- Add negative tests for anonymous, non-member, wrong-role, removed-member, and cross-household access where relevant.
- Never log or expose access tokens, authorization codes, invitation tokens, SAS URLs, push endpoints/secrets, attachment contents, or unnecessary personal data.

## Data and reliability

- Apply database migrations explicitly through developer/deployment workflows; the API must not mutate production schemas during normal startup.
- Store cross-module references as scalar IDs without ORM navigation properties or cross-schema cascades.
- Preserve historical meaning through append-only evidence or versioned state where required.
- Replayable commands use the approved operation-ID/idempotency contract.
- Durable cross-module events use transactional outbox and idempotent inbox semantics.
- Background workers use bounded batches, leases, cancellation, explicit retry classification, backoff, terminal failure handling, observability, and restart recovery.
- Do not describe external delivery as exactly once.

## PWA

- Keep service-worker asset caching separate from offline business-data synchronization.
- Isolate browser-persisted queued data by authenticated user and household; never persist tokens in the action queue.
- Provide explicit loading, empty, offline, pending, conflict, validation, authorization, and terminal-error states.
- Preserve accessibility, keyboard/focus behavior, touch targets, responsive layout, and browser-specific constraints.

## Testing and quality

Use the smallest complete portfolio for the changed risk:
- xUnit v3 on Microsoft Testing Platform v2 for domain/application/integration behavior;
- real PostgreSQL at persistence boundaries;
- bUnit for Razor component behavior;
- Playwright Chromium for published critical browser journeys;
- ArchUnitNET/reflection assertions for assembly and dependency rules;
- failure-injection/restart tests for idempotency, workers, and external-provider behavior;
- Cobertura coverage artifacts as diagnostic evidence, not a substitute for meaningful assertions.

Builds use nullable analysis, recommended analyzers, code-style enforcement, warnings as errors, deterministic output, central package management, and NuGet vulnerability auditing. Do not suppress warnings or advisories without a narrow documented justification.

## Pull-request review behavior

When performing a code review:
- verify the linked HK issue outcome, dependencies, exclusions, and acceptance criteria;
- prioritize correctness, security, data integrity, authorization, concurrency, restart behavior, and migration safety over style;
- inspect tests and operational/documentation impact, not only production code;
- report actionable findings with severity, file/line evidence, failure mode, smallest safe correction, and proof required;
- distinguish defects from questions and non-blocking suggestions;
- avoid speculative abstractions, unrelated refactors, vague comments, and praise-only noise;
- never approve only because CI is green;
- state explicitly when no material findings exist and identify residual manual validation gaps.

For a dedicated review session, select the `housekeeper-reviewer` custom agent defined in `.github/agents/housekeeper-reviewer.agent.md`.