# Release promotion runbook

This is the authoritative branch and workflow contract for moving HouseKeeper
from development to production. The normal product inner loop is cloud-free.
The only AWS apply paths are the protected `shared-development` and
`production` GitHub environments.

## Branches, tags, and protection

The lifecycle is:

```text
feature/* -> development -> rc/vX.Y.Z -> release/vX.Y.Z -> master -> vX.Y.Z
```

- Feature branches merge into `development` through a reviewed PR.
- A protected annotated or lightweight `rc/vX.Y.Z` tag must point to a commit
  reachable from `development`.
- The tag workflow creates `release/vX.Y.Z` at that exact commit and creates
  one PR into `master`.
- The release PR is the only normal path from `release/**` to `master`.
- After successful production deployment, the merge commit on `master` receives
  the final `vX.Y.Z` tag and a GitHub Release.
- A failed production deployment creates neither the final tag nor the release.

Repository administrators must protect `development`, `release/**`, and
`master` with required pull requests, the checks from their applicable
validation workflow, conversation resolution, and no force pushes or branch
deletion. Protect `rc/*` and `v*` tags against creation and update. Restrict
the `shared-development` and `production` environments to their intended
branches and require deliberate reviewers. These GitHub settings are external
repository controls and must be checked during release-readiness review.

## Workflow map

| Workflow | Trigger | AWS behavior |
|---|---|---|
| `ci.yml` -> `validate-development-pr.yml` | PR targeting `development` | No AWS credentials; build, tests, migration-backed API smoke, and no publishing |
| `ci.yml` -> `validate.yml` | Push to `development`, `release/**`, or `master` | No AWS credentials; full validation and release artifacts where applicable |
| `ci-release.yml` -> `validate.yml` | PR targeting `release/**` or `master` | No AWS credentials; full validation and release artifacts where applicable |
| `codex-review-request.yml` | `pull_request_target` for every PR targeting `development` | Requests a generated description and review; never checks out or executes PR code |
| `promote-release-candidate.yml` | `rc/vX.Y.Z` tag push | Verifies ancestry and creates the release branch/PR; no AWS |
| `release-preproduction.yml` | Manual dispatch for a release branch | Application-only changes skip AWS. Infrastructure or migration changes fail unless `enable_preproduction=true`; an enabled run deploys the exact candidate to `shared-development` |
| `deploy-development.yml` | Manual dispatch | Calls the protected shared-development deployment with an exact candidate run |
| `deploy-production.yml` | Merged release PR targeting `master` | Locates the successful release-head candidate, deploys to protected `production`, then creates the final tag and GitHub Release |
| `deploy-environment.yml` | Reusable workflow call | Performs CDK, image, migration, readiness, PWA, smoke, and restart/recovery steps for the selected protected environment |

The candidate is generated only by a successful validation of
`release/vX.Y.Z` (the promotion workflow dispatches validation when branch
creation does not produce a push event). Its metadata binds the artifact to the source SHA, source
tree SHA, release version, source ref, and API image identity. Promotion looks
up the validation run by exact SHA and verifies the run event, branch,
conclusion, artifact metadata, and image identity before AWS credentials are
used.

## Human promotion procedure

1. Complete the local validation below and open the PR into `development`.
2. Resolve or explicitly disposition all Codex findings and obtain human
   approval.
3. Create `rc/vX.Y.Z` from the current `development` commit. The tag workflow
   creates the release branch and promotion PR.
4. Wait for the release branch validation to pass. Record its run ID and
   candidate SHA in the release PR.
5. For application-only changes, record that shared-development was skipped.
   For infrastructure, container, migration, workflow, or deployment-script
   changes, manually dispatch `release-preproduction.yml` with the exact
   release branch and SHA and set `enable_preproduction=true`.
6. Review the shared-development migration, readiness, authenticated smoke,
   browser/manual journey, and recovery evidence. Do not approve the release
   PR while the required pre-production gate is missing.
7. Merge the release PR into `master` using the protected merge policy.
8. The production workflow deploys the exact release-head candidate. A human
   approves the `production` environment when prompted.
9. Confirm migration, readiness, authenticated smoke, persistence after ECS
   restart, image digest, and deployment evidence. The workflow then creates
   `vX.Y.Z` and the GitHub Release.

## Local validation and recovery

Run the same meaningful checks before opening or promoting a PR:

```bash
dotnet tool restore
dotnet restore HouseKeeper.slnx
dotnet build HouseKeeper.slnx --configuration Release --no-restore
dotnet test tests/Modules/HouseKeeper.Modules.Households.Tests/HouseKeeper.Modules.Households.Tests.csproj --configuration Release --no-build
dotnet test tests/HouseKeeper.Web.Tests/HouseKeeper.Web.Tests.csproj --configuration Release --no-build
dotnet test tests/HouseKeeper.ArchitectureTests/HouseKeeper.ArchitectureTests.csproj --configuration Release --no-build
dotnet test deploy/aws/tests/HouseKeeper.Infrastructure.Tests/HouseKeeper.Infrastructure.Tests.csproj --configuration Release --no-build
npm install --global aws-cdk@2.1132.1
cd deploy/aws
cdk synth --strict
cd ../..
bash scripts/dev.sh
bash scripts/smoke.sh
```

Use `pwsh ./scripts/dev.ps1` instead of the Bash topology command on Windows.
Local commands never assume AWS credentials and never deploy a GitHub branch.

For a failed validation, open the run's `housekeeper-validation-*` artifact and
inspect restore, build, test, migration, API, web, Playwright, CDK, and restart
logs. For a failed protected deployment, download the
`housekeeper-deployment-*` artifact, inspect the CDK diff, migration task and
readiness evidence, and review CloudFormation/ECS events in the protected AWS
account. Retry only the same verified candidate after the cause is understood.

Do not run destructive down-migrations during rollback. ECS circuit-breaker
rollback or a deliberate redeploy of the previous image digest restores the
application artifact. Database rollback uses a compatible forward migration or
an approved RDS snapshot restore; it is never an automatic `down` migration.
If a migration task fails, leave the service stopped, preserve its task logs,
fix or supersede the reviewed migration, and promote a new candidate. If the
workflow stops after deployment but before tagging, verify the deployed digest
and rerun the production workflow only with the same source and candidate run.

## Cost and secret boundaries

Shared-development is one reused environment in `af-south-1`; release branches
do not create accounts, VPCs, RDS instances, buckets, or distributions. Its
protected workflow serializes deployments and uses the CDK non-production
retention/removal policies. Production uses retained state, deletion
protection, longer backups, and a separate environment-scoped OIDC role.

Only public endpoint and Cognito client configuration is written to PWA assets.
OIDC credentials are short-lived. Smoke access tokens, database credentials,
codes, and private object contents remain protected secrets and are not placed
in logs or artifacts.
