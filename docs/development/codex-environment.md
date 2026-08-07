# HouseKeeper Codex review environment

This runbook defines the Codex cloud environment used for HouseKeeper pull-request review and repository-aware implementation tasks. The environment is intentionally capable of restoring, building, testing, migrating, synthesizing infrastructure, and running the browser journey without receiving AWS credentials or production secrets.

The deterministic GitHub Actions pipeline remains the source of truth for merge evidence. The Codex environment exists to let the reviewer reproduce the smallest relevant subset of that evidence while investigating a change.

## Environment profile

Create one Codex cloud environment with the following values.

| Setting | Value |
|---|---|
| Name | `HouseKeeper PR Review` |
| Repository | `JohannesMogashoa/housekeeper` |
| Normal review base | `development` |
| Node.js | `22` |
| Setup script | `bash scripts/codex-setup.sh` |
| Maintenance script | `bash scripts/codex-maintenance.sh` |
| Agent internet access | `Off` |
| Secrets | None |

The setup script has internet access during environment provisioning and installs the dependencies that are absent from the Codex universal image. The reviewer itself should remain network-disabled unless a specific investigation proves that limited outbound access is necessary.

## Environment variables

Configure these as ordinary environment variables, not secrets:

```text
ASPNETCORE_ENVIRONMENT=Development
DOTNET_NOLOGO=true
DOTNET_CLI_TELEMETRY_OPTOUT=true
ConnectionStrings__HouseKeeper=Host=127.0.0.1;Port=5432;Database=housekeeper;Username=housekeeper;Password=housekeeper_codex;Include Error Detail=true
HOUSEKEEPER_ENVIRONMENT=development
HOUSEKEEPER_AWS_REGION=af-south-1
HOUSEKEEPER_GITHUB_REPOSITORY=JohannesMogashoa/housekeeper
```

`housekeeper_codex` is a disposable container-local PostgreSQL password, not an application, AWS, GitHub, or production secret. Do not add AWS access keys, Cognito credentials, deployment-role credentials, smoke access tokens, database production credentials, or package-registry credentials to this environment.

## What setup installs

`scripts/codex-setup.sh` prepares the review container with:

- .NET SDK `10.0.300`, matching `global.json` and CI;
- Node.js `22`, matching the full validation workflow;
- AWS CDK CLI `2.1132.1`, matching the full validation workflow;
- PostgreSQL `18` from the official PGDG repository;
- PowerShell, required by the .NET Playwright installer;
- repository-local `dotnet-ef` from `.config/dotnet-tools.json`;
- restored NuGet dependencies;
- the pinned Playwright Chromium browser and Linux dependencies.

The setup intentionally does not install or authenticate the AWS CLI for deployment, configure GitHub credentials, or obtain access to shared-development/production resources.

### PostgreSQL parity note

GitHub Actions runs the exact `postgres:18.4` image. Codex cloud runs directly inside a container and should not depend on a nested Docker daemon for review correctness, so the review environment installs the current PostgreSQL 18 package from PGDG instead.

This gives Codex the correct PostgreSQL major-version semantics for migrations, EF Core behavior, SQL, locking, and persistence investigation. The exact 18.4 service-container run in GitHub Actions remains authoritative before merge.

## Cached environment maintenance

Codex may reuse a cached environment after checking out a different branch. `scripts/codex-maintenance.sh` therefore:

1. restores the .NET shell path;
2. restarts the local PostgreSQL 18 cluster if required;
3. reasserts the disposable review database contract;
4. restores repository-local .NET tools;
5. restores branch-specific NuGet dependencies.

If the cached container no longer contains the expected SDK, PostgreSQL cluster, CDK CLI, PowerShell installation, or Playwright browser, reset the Codex environment cache instead of teaching the maintenance script to repair arbitrary machine drift.

## Review execution strategy

Codex should read the root and applicable nested `AGENTS.md` files before selecting validation commands. Do not run the entire test portfolio mechanically for every pull request; run the smallest complete portfolio that covers the changed risk, then rely on CI for deterministic whole-pipeline evidence.

### Baseline for ordinary application changes

```bash
dotnet tool restore
dotnet restore HouseKeeper.slnx
dotnet build HouseKeeper.slnx --configuration Release --no-restore

dotnet test tests/Modules/HouseKeeper.Modules.Households.Tests/HouseKeeper.Modules.Households.Tests.csproj \
  --configuration Release \
  --no-build

dotnet test tests/HouseKeeper.Web.Tests/HouseKeeper.Web.Tests.csproj \
  --configuration Release \
  --no-build
dotnet test tests/HouseKeeper.ArchitectureTests/HouseKeeper.ArchitectureTests.csproj \
  --configuration Release \
  --no-build
dotnet test deploy/aws/tests/HouseKeeper.Infrastructure.Tests/HouseKeeper.Infrastructure.Tests.csproj \
  --configuration Release \
  --no-build
```

### Persistence, EF Core, or migration changes

In addition to the relevant tests, apply the migrations against the disposable local PostgreSQL database:

```bash
dotnet ef database update \
  --project src/Modules/HouseKeeper.Modules.Households \
  --startup-project src/HouseKeeper.Api \
  --context HouseholdsDbContext \
  --configuration Release \
  --no-build
```

Review migration SQL, indexes, constraints, compatibility, transaction behavior, and rollback/recovery implications. Do not treat a successful migration command as sufficient evidence by itself.

### API, authorization, or household-isolation changes

After build and migration, run the API and smoke journey:

```bash
dotnet run \
  --project src/HouseKeeper.Api/HouseKeeper.Api.csproj \
  --configuration Release \
  --no-build \
  --no-restore \
  -- --urls http://127.0.0.1:5287
```

In a second shell:

```bash
bash scripts/smoke.sh
```

Add targeted negative tests for anonymous, non-member, wrong-role, removed-member, and cross-household behavior when the changed boundary requires them.

### PWA or critical browser changes

Publish and serve the application, then run the end-to-end project using the Chromium binary cached during setup. The repository's full validation workflow is the canonical command sequence.

At minimum, inspect and test:

- browser-local identity/session isolation;
- service-worker/update behavior when touched;
- offline/recovery states when touched;
- accessibility and keyboard behavior;
- mobile layout for visible changes;
- household isolation across browser-visible data.

### AWS CDK or infrastructure changes

No AWS credentials are required for review-time synthesis and policy checks:

```bash
cd deploy/aws
cdk synth --strict
```

Also run the infrastructure tests and inspect generated CDK Nag reports. Review IAM scope, trust policies, public exposure, encryption, retention/deletion policy, migration-role separation, rollback behavior, and cost-impacting resources.

Do not run `cdk deploy`, `cdk destroy`, assume a deployment role, or contact production from Codex review tasks.

### Container-definition changes

Codex cloud review should inspect Dockerfiles and can reason about container configuration, but the GitHub Actions runner remains authoritative for `docker build` because the review environment does not require a nested Docker daemon.

Validate at least:

- non-root runtime behavior;
- immutable/pinned base-image intent;
- exposed ports and health checks;
- secret-free build arguments and layers;
- runtime file ownership and writable paths;
- image/runtime compatibility with ECS Fargate.

## Internet-access policy

Keep agent internet access `Off` by default.

The setup phase already has network access for .NET, NuGet, npm, PGDG, Microsoft PowerShell packages, and Playwright downloads. The review phase normally needs only repository contents and those cached dependencies.

If a review genuinely requires external documentation, prefer a separate evidence-gathering step or temporarily enable a narrow domain allowlist with read-only HTTP methods. Never enable unrestricted internet merely to make a build work; missing build dependencies belong in the setup script.

## Security boundary

The Codex environment is a review sandbox, not a deployment workstation.

It must never contain:

- AWS access keys or long-lived credentials;
- GitHub personal access tokens;
- `HOUSEKEEPER_SMOKE_ACCESS_TOKEN`;
- Cognito test-user passwords or authorization codes;
- production/shared-development RDS secrets;
- private household data or uploaded files;
- environment-protection bypass credentials.

Infrastructure review uses static synthesis and policy evidence. Protected GitHub environments, OIDC trust, required reviewers, and the deployment workflows remain the only path to shared-development and production AWS changes.

## Codex review configuration

Repository review behavior is defined by `AGENTS.md` and the nested instruction files. The environment provides executable evidence; it does not replace those instructions.

For GitHub reviews:

1. connect the repository to Codex;
2. select `HouseKeeper PR Review` as the repository environment;
3. enable automatic review for pull requests targeting `development` if desired;
4. keep the existing `@codex review` workflow for explicit/idempotent requests;
5. require material Codex findings to be resolved or deliberately dispositioned before human approval.

The reviewer should report prioritized, actionable findings and should not modify the working tree when operating in review mode.

## Verification after environment creation

Use a small pull request that changes no production behavior and confirm that Codex can:

```bash
dotnet --version
node --version
cdk --version
psql --version
pwsh --version
pg_isready --host 127.0.0.1 --port 5432

dotnet restore HouseKeeper.slnx
dotnet build HouseKeeper.slnx --configuration Release --no-restore
```

Then request:

```text
@codex review
```

The review is considered correctly wired when Codex reads the repository `AGENTS.md` hierarchy, can execute the relevant local validation without AWS credentials, posts findings against the pull request, and the normal GitHub Actions checks remain independent.
