# Architecture Decision Record Index

This index is the code-adjacent catalogue of HouseKeeper architecture decisions. The originating Notion task remains the detailed research and approval record; this repository records the decision that governs the implementation.

A future change that materially contradicts an accepted decision must add a superseding ADR rather than silently editing history.

| ADR | HK task | Decision | Status | Repository evidence |
|---|---:|---|---|---|
| [0001](#adr-0001-responsive-pwa-first) | HK-2 | Responsive Blazor PWA first; staged MAUI host only after a measured trigger | Accepted; physical-device validation outstanding | Standalone installable PWA in HK-14 |
| [0002](#adr-0002-net-10-and-application-model) | HK-3 | .NET 10 LTS, standalone Blazor WebAssembly, separate ASP.NET Core API | Accepted | `global.json`, Web and API projects |
| [0003](#adr-0003-modular-monolith) | HK-4 | Modular monolith with module-owned rules, contracts and schemas | Accepted | Households module and architecture tests |
| [0004](#adr-0004-repository-and-solution-structure) | HK-5 | One repository, SLNX solution, host/module-first organisation, central packages | Accepted | Current repository layout and build policy |
| [0005](#adr-0005-postgresql-ef-core-and-migrations) | HK-6 | PostgreSQL, EF Core/Npgsql, module contexts and explicit migrations | Accepted | `households` schema and clean-database CI migration |
| [0006](#adr-0006-identity-and-household-authorization) | HK-7 | Entra External ID for authentication; application-owned household memberships for authorization | Superseded by ADR-0014; membership boundary retained | Development authentication exercises the boundary |
| [0007](#adr-0007-recurring-task-model) | HK-8 | Versioned routines, materialized occurrences and immutable completion history | Accepted; implementation pending | Foundation backlog |
| [0008](#adr-0008-reminders-and-background-processing) | HK-9 | PostgreSQL-backed durable dispatch with EventBridge/SQS provider ingress where required | Accepted; implementation pending | Foundation backlog |
| [0009](#adr-0009-attachment-storage) | HK-10 | Private S3, exact-object SigV4 presigning, GuardDuty scanning and module-owned metadata | Supersedes Azure storage choice in ADR-0009; implementation pending | Foundation backlog |
| [0010](#adr-0010-automated-testing) | HK-11 | xUnit v3/MTP v2, bUnit, Playwright, ArchUnitNET and PostgreSQL integration tests | Accepted | HK-14 CI and test projects |
| [0011](#adr-0011-local-orchestration) | HK-12 | Defer .NET Aspire; use scripts, Docker Compose and direct OpenTelemetry integration | Deferred | `scripts/`, `deploy/local/compose.yaml`; POC PR closed |
| [0012](#adr-0012-hosting-and-delivery) | HK-13 | S3/CloudFront, ECS Fargate/ALB, RDS PostgreSQL, CDK and GitHub Actions OIDC | Superseded by ADR-0014; AWS implementation pending | Foundation backlog |
| [0013](#adr-0013-walking-skeleton) | HK-14 | Prove the architecture through one authenticated persisted vertical slice | Accepted and implemented | PR #2 merged to `master` |
| [0014](#adr-0014-aws-platform) | HK-28 | AWS-first platform in `af-south-1` with Cognito, ECS, S3, RDS, CDK and OIDC | Accepted; implementation in progress | CDK skeleton and platform documentation |
| [0015](#adr-0015-aws-observability) | HK-28 | CloudWatch and X-Ray/OpenTelemetry provide the operational telemetry baseline | Accepted; implementation pending | AWS CDK observability stack |

## ADR-0001: Responsive PWA first

**Decision:** Ship one mobile-first responsive Blazor WebAssembly PWA. Preserve host-agnostic contracts and components, but do not build a MAUI client during MVP.

**Revisit when:** native-only capabilities, app-store distribution, advanced device integration, background upload or browser-related support incidents become material.

**Source:** [HK-2 in Notion](https://app.notion.com/p/3a1decef1da1810180f8fe7f3b9bc708)

## ADR-0002: .NET 10 and application model

**Decision:** Target .NET 10 LTS and `net10.0`; use a standalone Blazor WebAssembly PWA and a separate ASP.NET Core API with versioned JSON contracts.

**Source:** [HK-3 in Notion](https://app.notion.com/p/3a1decef1da181fb8e18d7c510c22738)

## ADR-0003: Modular monolith

**Decision:** Use one backend deployable composed of capability modules. Each module owns its domain and persistence; implementation assemblies do not reference one another.

**Source:** [HK-4 in Notion](https://app.notion.com/p/3a1decef1da18139a16ff78b0a2e1324)

## ADR-0004: Repository and solution structure

**Decision:** Use one Git repository and one SLNX solution. Create project boundaries for deployables, modules, stable cross-boundary contracts and independent test suites—not for trivial abstractions.

**Source:** [HK-5 in Notion](https://app.notion.com/p/3a1decef1da181209682c5e4655597b9)

## ADR-0005: PostgreSQL, EF Core and migrations

**Decision:** Use PostgreSQL through EF Core/Npgsql. Persistent modules own schemas, contexts and migration histories. Apply reviewed migrations outside normal API startup.

**Source:** [HK-6 in Notion](https://app.notion.com/p/3a1decef1da181dcb38ef13d9964b40c)

## ADR-0006: Identity and household authorization

**Decision:** Microsoft Entra External ID authenticates production users. HouseKeeper maps external subjects to members and enforces application-owned household memberships and roles in the API.

**Status:** Superseded by ADR-0014 for the external provider. The application-owned subject mapping and household membership authorization boundary remain accepted.

**Source:** [HK-7 in Notion](https://app.notion.com/p/3a1decef1da181218229f396cddb4319)

## ADR-0007: Recurring task model

**Decision:** Separate versioned routine definitions from materialized task occurrences. Occurrences own due state, assignment and completion/postponement history so schedule changes do not rewrite history.

**Source:** [HK-8 in Notion](https://app.notion.com/p/3a1decef1da181c78556d8675ae8958b)

## ADR-0008: Reminders and background processing

**Decision:** Store notification dispatch state in PostgreSQL and process it through a leased, idempotent BackgroundService inside the initial API host. EventBridge and SQS may provide managed ingress for external/provider events, but application inboxes remain the durability and idempotency boundary. Web Push is optional; the in-app notification centre is the durable fallback.

**Source:** [HK-9 in Notion](https://app.notion.com/p/3a1decef1da1814cba73d7f0ada734ef)

## ADR-0009: Attachment storage

**Decision:** Store private binary content in Amazon S3 and metadata/lifecycle state in PostgreSQL. The API grants exact-object short-lived SigV4 presigned operations after authorization and quota checks. GuardDuty Malware Protection for S3 and application validation must complete before content is linkable or downloadable.

**Status:** The S3 choice supersedes the historical Azure storage choice. The module ownership and lifecycle rules remain accepted.

**Source:** [HK-10 in Notion](https://app.notion.com/p/3a1decef1da18168a07ef55309d22785)

## ADR-0010: Automated testing

**Decision:** Use xUnit v3 on Microsoft Testing Platform v2 for .NET tests, bUnit for Razor components, Playwright for critical browser journeys, ArchUnitNET/reflection assertions for dependency rules, real PostgreSQL at integration boundaries, and Cobertura coverage artifacts.

**Source:** [HK-11 in Notion](https://app.notion.com/p/3a1decef1da18195beecc810cb10017a)

## ADR-0011: Local orchestration

**Decision:** Defer .NET Aspire while there is one backend process. Use ordinary `dotnet` execution, repository scripts, Docker Compose, Testcontainers and direct OpenTelemetry/health-check integration.

**Revisit when:** at least two independently executable backend processes exist or startup/telemetry coordination becomes a concrete recurring problem.

**Source:** [HK-12 in Notion](https://app.notion.com/p/3a1decef1da18168ba41daa98bab3756)

## ADR-0012: Hosting and delivery

**Decision:** This Azure-first decision is superseded by ADR-0014. Its immutable-artifact, explicit-migration, protected-promotion and rollback principles remain applicable.

**Source:** [HK-13 in Notion](https://app.notion.com/p/3a1decef1da1814896cbde631651bb38)

## ADR-0013: Walking skeleton

**Decision:** The architecture is not considered selected merely because it is documented. It must be demonstrated by an end-to-end slice that authenticates a development subject, creates a household and owner membership atomically, persists to PostgreSQL, survives browser and API restart, and passes the full CI portfolio.

**Evidence:** [PR #2](https://github.com/JohannesMogashoa/housekeeper/pull/2)

## ADR-0014: AWS platform

**Decision:** Pivot the cloud platform from Azure to AWS while preserving the .NET 10 modular monolith, standalone Blazor WebAssembly PWA, ASP.NET Core API, PostgreSQL persistence, module-owned schemas, and application-owned household authorization. Use `af-south-1` as the primary region. Host the PWA from a private S3 bucket behind CloudFront Origin Access Control; run the API as a hardened non-root container on ECS Fargate behind an ALB; use ECR with immutable tags and scanning; use RDS for PostgreSQL; use Cognito User Pools with authorization-code flow and PKCE; use private S3 with SigV4 presigned grants and GuardDuty Malware Protection for S3 for attachments; use Secrets Manager and Parameter Store; use CloudWatch/X-Ray/OpenTelemetry; and deploy through C# AWS CDK and GitHub Actions OIDC to narrow IAM roles.

**Consequences:** AWS becomes the active platform vocabulary and deployment target. Provider-neutral domain and application code remains provider-neutral. CDK stacks must keep runtime and migration privileges separate, use deletion protection for production state, retain cost controls, and prove deployment safety through synth, policy checks, reviewed diffs and smoke evidence. Azure/Entra/Bicep documents remain historical only where explicitly marked superseded.

**Rejected alternatives:** Retain Azure as the primary platform, use AWS App Runner, use EKS or a service mesh, use Cognito Identity Pools or browser-issued AWS credentials, and use Cognito groups or IAM roles for household authorization.

**Source:** [HK-28 / GitHub issue #20](https://github.com/JohannesMogashoa/housekeeper/issues/20)

## ADR-0015: AWS observability

**Decision:** Export structured application logs to CloudWatch Logs, publish actionable CloudWatch metrics and alarms for health, latency, errors, database saturation, queue age, retries, authorization denials and deployment health, and propagate OpenTelemetry-compatible traces to X-Ray. Do not include tokens, credentials, authorization codes, private files, household-sensitive data or high-cardinality subjects in telemetry.

**Source:** ADR-0014 and HK-28.
