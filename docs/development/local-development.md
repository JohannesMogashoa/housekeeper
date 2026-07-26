# Local Development Guide

This guide is the supported local-development contract for the HouseKeeper foundation baseline.

## Prerequisites

- .NET SDK `10.0.300` or a compatible .NET 10 feature-band roll-forward
- Docker Desktop, Rancher Desktop, Podman Desktop, or another Docker Compose-compatible OCI runtime
- Bash on macOS/Linux or PowerShell 7 on Windows
- Git
- A modern Chromium-based browser for the automated browser journey
- Node.js 18 or later when running the AWS CDK synthesis checks

Repository manifests pin the .NET SDK line, local .NET tools, NuGet package versions and test platform. Do not install PostgreSQL directly for the normal inner loop.

## Clone and verify the toolchain

```bash
git clone https://github.com/JohannesMogashoa/housekeeper.git
cd housekeeper
dotnet --info
docker version
docker compose version
dotnet tool restore
```

On Windows, run the equivalent commands from PowerShell 7.

## Start the complete local topology

### macOS or Linux

```bash
bash scripts/dev.sh
```

### Windows

```powershell
pwsh ./scripts/dev.ps1
```

The command:

1. starts PostgreSQL 18.4 and the pinned MinIO S3-compatible emulator through `deploy/local/compose.yaml`;
2. restores repository tools and packages;
3. waits for database readiness;
4. applies the Households migrations explicitly;
5. starts the API at `http://localhost:5287`;
6. starts the PWA at `http://localhost:5136`.

Open `http://localhost:5136`, choose `Sign in`, enter a development display name,
and create a household. The page labels this as Development sign-in so it is
clear that this is a local learning path rather than production authentication.

The development identity is deliberately local-only. It exercises the same authentication and authorization middleware boundary that the production identity provider will use, but it is not production authentication.

## Cognito authentication modes

The repository has three intentional modes:

- `Development`: local headers and a browser-local synthetic identity; only the
  API `Development` environment can activate this mode.
- `CI`: deterministic API signing keys and component/browser contract tests;
  CI does not depend on a shared Cognito user or AWS credentials.
- `shared-development`: Cognito managed login with authorization code + PKCE,
  a disposable test user, and the environment-specific public client settings.

For shared development, configure the PWA public values from the CDK
`HouseKeeperIdentity` outputs: hosted-login authority, web client ID, API
base URL, callback URL, and logout URL. These values are safe for browser
configuration; client secrets and AWS credentials are never required.

The sign-in page intentionally explains that Cognito verifies identity while
HouseKeeper membership rows determine household access. Passwords, recovery
codes, access tokens, and refresh tokens must remain out of logs and support
artifacts.

The local S3-compatible endpoint is `http://localhost:9000` with console
`http://localhost:9001`. It is for provider-neutral attachment tests only and
must never be used with production credentials or data.

## Stop or reset

Stop the foreground application processes with `Ctrl+C`.

PostgreSQL and local S3 data remain in named Docker volumes. To remove both and start from a clean state:

```bash
docker compose -f deploy/local/compose.yaml down --volumes
```

To stop containers without deleting data:

```bash
docker compose -f deploy/local/compose.yaml down
```

## Apply migrations explicitly

```bash
dotnet tool restore
dotnet ef database update \
  --project src/Modules/HouseKeeper.Modules.Households \
  --startup-project src/HouseKeeper.Api \
  --context HouseholdsDbContext
```

The API does not apply production schema changes during startup. New modules must follow the same rule and own their context, schema and migration history.

## AWS deployment foundation

The repository's approved shared and production platform is AWS in `af-south-1`. Local development remains cloud-independent and uses Docker Compose PostgreSQL plus the Development-only identity. Do not use AWS credentials for the normal inner loop.

From the repository root, the infrastructure checks are:

```bash
dotnet restore deploy/aws/HouseKeeper.Infrastructure.csproj
dotnet build deploy/aws/HouseKeeper.Infrastructure.csproj --configuration Release --no-restore
dotnet test deploy/aws/tests/HouseKeeper.Infrastructure.Tests/HouseKeeper.Infrastructure.Tests.csproj --configuration Release
npm install --global aws-cdk@2.1132.1
cd deploy/aws
cdk synth --strict
```

The CDK app defaults to a development-safe configuration. Shared or production synthesis requires explicit region, account, callback URLs, repository identity, certificate and domain inputs. Deployment requires GitHub OIDC or an equivalent short-lived AWS role; long-lived access keys and committed secret values are not supported.

## Standard verification

```bash
dotnet tool restore
dotnet restore HouseKeeper.slnx
dotnet build HouseKeeper.slnx --configuration Release --no-restore
```

### Domain and module tests with coverage

```bash
dotnet test tests/Modules/HouseKeeper.Modules.Households.Tests/HouseKeeper.Modules.Households.Tests.csproj \
  --configuration Release \
  --no-build \
  -- \
  --coverage \
  --coverage-output artifacts/test-results/households.cobertura.xml \
  --coverage-output-format cobertura
```

### Component tests

```bash
dotnet test tests/HouseKeeper.Web.Tests/HouseKeeper.Web.Tests.csproj \
  --configuration Release \
  --no-build
```

### Architecture tests

```bash
dotnet test tests/HouseKeeper.ArchitectureTests/HouseKeeper.ArchitectureTests.csproj \
  --configuration Release \
  --no-build
```

### API smoke journey

Start the local topology, then run:

```bash
bash scripts/smoke.sh
```

### Browser journey

Build the Playwright test project and install the pinned browser once:

```bash
dotnet build tests/HouseKeeper.EndToEndTests/HouseKeeper.EndToEndTests.csproj \
  --configuration Release

pwsh tests/HouseKeeper.EndToEndTests/bin/Release/net10.0/playwright.ps1 \
  install --with-deps chromium
```

With the application running:

```bash
HOUSEKEEPER_WEB_BASE_URL=http://localhost:5136 \
  dotnet test tests/HouseKeeper.EndToEndTests/HouseKeeper.EndToEndTests.csproj \
    --configuration Release \
    --no-build
```

## Configuration policy

- Commit non-secret defaults in `appsettings.json` or equivalent static configuration.
- Keep developer secrets in .NET user secrets or ignored local environment files.
- Never commit database passwords, identity-provider secrets, storage credentials, SAS tokens or production connection strings.
- The PWA may contain public configuration such as an API base URL or public identity client identifier, but never privileged secrets.
- Environment-specific options must be validated during application startup.

## Local service ports

| Service | Default local endpoint |
|---|---|
| PWA | `http://localhost:5136` |
| API | `http://localhost:5287` |
| PostgreSQL | `localhost:54329` |

The scripts are the authoritative source when ports change.

## Troubleshooting

### PostgreSQL is not ready

```bash
docker compose -f deploy/local/compose.yaml ps
docker compose -f deploy/local/compose.yaml logs postgres
```

Reset the volume only when local data may be discarded.

### Port already in use

Inspect the process that owns ports `5136`, `5287` or `54329`, then stop that process or adjust the local configuration consistently across scripts and app settings.

### Migration fails after switching branches

Compare the current migration set with the database state. For disposable local data, reset the Compose volume and rerun the supported startup command. Do not manually edit the EF migrations history table.

### Browser journey cannot find Chromium

Rebuild the end-to-end project and rerun the generated `playwright.ps1 install --with-deps chromium` command for the current build output.

### Restore reports a vulnerability

Do not suppress the audit warning merely to unblock the build. Determine whether the dependency is direct or transitive, upgrade or pin a patched compatible version, and record the reason when a transitive override is necessary.

## Adding a module

A new persistent module must provide:

- a `HouseKeeper.Modules.<Capability>` assembly;
- internal Domain, Application, Infrastructure and Endpoints areas as required;
- a module registration surface for the API composition root;
- its own PostgreSQL schema, `DbContext` and migration history;
- public boundary records in `HouseKeeper.Contracts` only when another process or module requires them;
- focused module tests and architecture-test coverage;
- no reference to another module implementation assembly.

Review the [technical recommendation](../architecture/technical-recommendation.md) and [ADR index](../architecture/adr/README.md) before introducing a dependency that changes an approved boundary.
