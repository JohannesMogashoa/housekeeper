# HouseKeeper AWS infrastructure

This directory contains the C# AWS CDK application for HouseKeeper. The
initial region is `af-south-1`. `shared-development` is one reused,
disposable pre-production environment; `production` is isolated and retained.
Ordinary development and CI validation do not assume AWS credentials.

For the complete first-time setup sequence, including GitHub rulesets, Codex,
OIDC, ACM, SES, Cognito smoke access, environment variables, and secrets, see
the [GitHub project and deployment setup guide](../../docs/development/github-project-setup.md).

## Stacks and ownership

- `NetworkStack`: VPC, load-balancer subnets, private application subnets,
  isolated database subnets, and security groups.
- `DataStack`: encrypted PostgreSQL RDS, generated Secrets Manager
  credentials, backups, deletion protection in production, and the migration
  endpoint parameter.
- `IdentityStack`: Cognito User Pool, authorization-code PKCE web client, and
  API scopes. Household membership and roles remain application-owned.
- `StorageStack`: private PWA and attachment buckets, CloudFront Origin Access
  Control, and GuardDuty Malware Protection for S3.
- `ApplicationStack`: immutable scan-on-push ECR, non-public ECS Fargate API
  tasks, ALB readiness checks, ECS Exec, separate runtime/migration roles, and
  deployment circuit-breaker rollback.
- `DeliveryStack`: the GitHub OIDC provider, an environment-scoped deployment
  role, a CloudFormation execution role, and resource-scoped artifact/task
  permissions.
- `ObservabilityStack`: CloudWatch logs and alarms, an X-Ray group, and a
  monthly cost budget.

## Local synthesis and review

~~~powershell
$env:HOUSEKEEPER_ENVIRONMENT = "development"
$env:HOUSEKEEPER_AWS_REGION = "af-south-1"
cd deploy/aws
dotnet restore
dotnet build --configuration Release --no-restore
cdk synth --strict
cdk diff
~~~

The full CI validation workflow used for protected-branch pushes and
`release/**`/`master` pull requests runs `cdk synth --strict`, CDK Nag policy
reports, the infrastructure test project, and repository cleanliness checks
without AWS credentials. Development pull requests use the smaller build,
test, and smoke workflow and do not set up CDK, publish artifacts, or contact
AWS. Protected apply workflows also record a `cdk diff` artifact; an
environment reviewer must inspect it before approval. Both protected
environments require an account, ACM certificate ARN, API domain, and explicit
Cognito callback and logout URLs. Stateful production resources retain state
and enable deletion protection.

## Protected environments and variables

Create two protected GitHub environments named exactly
`shared-development` and `production`. Restrict each environment to its
intended protected refs and configure required reviewers. Each environment
supplies these variables:

| Variable | Purpose |
|---|---|
| `HOUSEKEEPER_AWS_ACCOUNT` | Target AWS account ID |
| `HOUSEKEEPER_AWS_DEPLOY_ROLE_ARN` | Environment-scoped GitHub OIDC deployment role |
| `HOUSEKEEPER_CFN_EXECUTION_ROLE_ARN` | CloudFormation execution role passed by CDK |
| `HOUSEKEEPER_API_CERTIFICATE_ARN` | ACM certificate for the API HTTPS listener; required in production |
| `HOUSEKEEPER_API_DOMAIN_NAME` | DNS name routed to the API ALB; required in production |
| `HOUSEKEEPER_INVITATION_FROM_ADDRESS` | Verified SES sender used for household invitations |

Add `HOUSEKEEPER_SMOKE_ACCESS_TOKEN` as a protected environment secret. It is
used in memory for authenticated smoke and persistence checks and is never
printed or uploaded. The PWA receives only public API and Cognito client values.

The CDK environment variables used by protected jobs are:

~~~text
HOUSEKEEPER_ENVIRONMENT=shared-development or production
HOUSEKEEPER_GITHUB_ENVIRONMENT=shared-development or production
HOUSEKEEPER_AWS_REGION=af-south-1
HOUSEKEEPER_GITHUB_REPOSITORY=JohannesMogashoa/housekeeper
HOUSEKEEPER_API_IMAGE_URI=<ECR repository URI>@<immutable digest>
HOUSEKEEPER_API_DESIRED_COUNT=0 or 1
HOUSEKEEPER_COGNITO_CALLBACK_URLS=<semicolon-separated public callback URLs>
HOUSEKEEPER_COGNITO_LOGOUT_URLS=<semicolon-separated public logout URLs>
~~~

## OIDC trust model

`DeliveryStack` no longer trusts one branch such as `master`. Each environment
has a separate role whose trust requires all of:

~~~text
aud = sts.amazonaws.com
repository = JohannesMogashoa/housekeeper
sub = repo:JohannesMogashoa/housekeeper:environment:<environment-name>
~~~

The GitHub job must name the same protected environment. Branch restrictions
and required reviewers are enforced by the GitHub environment and branch
protection settings; the AWS role does not trust feature branches, arbitrary
pull-request refs, or a personal access key. The CloudFormation execution role
is trusted only by CloudFormation and is passed explicitly by the deployment
role.

Before the first protected apply, an account administrator bootstraps CDK in
`af-south-1` and performs the one-time trusted deployment that creates the
OIDC and CloudFormation roles:

~~~powershell
$env:HOUSEKEEPER_ENVIRONMENT = "shared-development"
$env:HOUSEKEEPER_GITHUB_ENVIRONMENT = "shared-development"
$env:HOUSEKEEPER_AWS_REGION = "af-south-1"
$env:HOUSEKEEPER_AWS_ACCOUNT = "<account-id>"
cd deploy/aws
cdk bootstrap aws://<account-id>/af-south-1
~~~

Record the emitted deployment and CloudFormation role ARNs in the protected
environment variables. Subsequent deployments use short-lived GitHub OIDC
credentials only.

## Artifact promotion and deployment order

The successful release-branch validation creates one candidate artifact named
`housekeeper-candidate-<source-sha>`. It includes the published API and PWA,
the API image tar, and `metadata.json` binding source SHA, tree SHA, release
version, source ref, and image ID. `deploy-environment.yml` verifies the
successful push/dispatch run and all metadata before configuring AWS credentials.

The protected apply order is:

1. Build and strictly synthesize the CDK source at the candidate SHA; record
   the reviewed diff.
2. Provision or update shared/prod infrastructure with the API service stopped.
3. Load the candidate image bytes and push the same image to the environment's
   immutable ECR repository; use the returned ECR digest in CDK.
4. Resolve the public PWA and API endpoints and register Cognito callback URLs.
5. Apply the exact image configuration while the service remains stopped.
6. Run the isolated ECS migration task with the separate migration role and
   stop on any non-zero exit code.
7. Start the API service, poll `/health/ready`, and publish the already-built
   PWA. Only public endpoint and Cognito client settings are injected into the
   PWA `appsettings.json`; the PWA is not rebuilt.
8. Run authenticated smoke and household persistence checks, force an ECS
   service restart, and repeat readiness and persistence checks.
9. Upload safe deployment evidence containing source/version/digest and logs.

The production workflow performs these steps only after the release PR merges
into `master`. It creates `vX.Y.Z` and the GitHub Release only after all steps
succeed. No final tag is created after a failed deployment.

## Optional shared-development pre-production

`release-preproduction.yml` classifies the diff from `master` to the exact
release head. Changes under CDK, containers, migrations, workflows, or AWS
smoke/deployment scripts are infrastructure-impacting. Such a release fails
the gate unless the dispatcher input `enable_preproduction=true` is explicitly
selected. Application-only releases report a deliberate AWS skip.

An enabled run calls the same reusable deployment workflow with
`environment=shared-development`; it does not create a new stack or duplicate
RDS, S3, CloudFront, Cognito, VPC, or ECS resources. Deployment concurrency is
serialized with `housekeeper-shared-development`.

## Migration, rollback, retry, and recovery

Normal API startup never changes the schema. Migrations run as a one-shot ECS
task using the migration role and the RDS secret through ECS secret injection.
The migration task must finish before the API service starts. Keep migrations
expand-and-contract compatible with the prior application during rollout.

On migration failure, leave the service stopped, retain the task diagnostics,
fix the forward migration in a new candidate, and retry only after review. Do
not run destructive EF down-migrations automatically. On application failure,
use ECS circuit-breaker rollback or redeploy the previous immutable ECR digest.
For a database incident, use the approved RDS snapshot/restore runbook and
then validate migrations and persistence before resuming traffic. A workflow
that fails after a successful deployment but before tagging may be retried
with the same exact source and candidate run after verifying the live digest.

Required evidence is the validation candidate metadata, CDK synth/diff and
policy reports, migration task result, readiness response, authenticated smoke
result, household persistence after restart, ECR digest, and CloudFormation/ECS
events. Do not store database passwords, Cognito passwords, access tokens,
private files, or household data in those artifacts.

## Cost limits and teardown

Shared-development is intentionally small and reused: one RDS instance, one
VPC, one ECS service, one ECR repository, one CloudFront distribution, and the
modeled storage/observability resources. Release branches do not create cloud
resources. Production uses deletion protection, retained state, Multi-AZ RDS,
longer backups, and a separate OIDC role. Review cost changes and the monthly
budget before enabling a new resource.

Shared-development teardown is disposable but requires a reviewed diff:

~~~powershell
$env:HOUSEKEEPER_ENVIRONMENT = "shared-development"
$env:HOUSEKEEPER_GITHUB_ENVIRONMENT = "shared-development"
$env:HOUSEKEEPER_AWS_REGION = "af-south-1"
cd deploy/aws
cdk destroy --all --force
~~~

Never destroy production state as a retry mechanism.
