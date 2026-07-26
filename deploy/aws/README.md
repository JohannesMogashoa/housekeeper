# HouseKeeper AWS infrastructure

This directory contains the C# AWS CDK application for HK-18. The shared
development target is `af-south-1`; environment-specific values are supplied
through `HOUSEKEEPER_*` variables or the standard CDK account context.

## Stacks

- `NetworkStack`: VPC, public load-balancer subnets, private application
  subnets, isolated database subnets, and security groups.
- `DataStack`: encrypted PostgreSQL RDS on a non-default endpoint port,
  generated Secrets Manager credentials, backups, deletion protection in
  production, and the migration endpoint parameter.
- `IdentityStack`: Cognito User Pool, authorization-code PKCE web client, and
  API scopes. Household membership and roles remain application-owned.
- `StorageStack`: private PWA and attachment buckets, CloudFront Origin Access
  Control, SigV4-compatible private object access, and GuardDuty Malware
  Protection for S3.
- `ApplicationStack`: immutable, scan-on-push ECR, non-public ECS Fargate API
  tasks, ALB health checks, ECS Exec, task roles, and deployment rollback.
- `DeliveryStack`: GitHub Actions OIDC provider and branch-scoped deployment
  role, a CloudFormation execution role, and resource-scoped artifact/task
  permissions for the protected deployment workflow.
- `ObservabilityStack`: CloudWatch logs and alarms, an X-Ray group, and a
  monthly cost budget.

## Synthesis and deployment

Install the pinned CDK Toolkit version from the repository guidance, then run:

```powershell
$env:HOUSEKEEPER_ENVIRONMENT = "development"
$env:HOUSEKEEPER_AWS_REGION = "af-south-1"
cd deploy/aws
dotnet restore
dotnet build --configuration Release --no-restore
cdk synth --strict
cdk diff
```

Production additionally requires an AWS account, an ACM certificate ARN,
an API domain name, and explicit Cognito callback/logout URLs. Review the
synthesized templates and `cdk diff` output before deployment. Production
stacks retain stateful resources and enable RDS deletion protection.

## Shared development workflow

The protected `.github/workflows/deploy-development.yml` workflow is the
supported apply path. Create a protected GitHub environment named
`shared-development` with these variables:

- `HOUSEKEEPER_AWS_ACCOUNT`
- `HOUSEKEEPER_AWS_DEPLOY_ROLE_ARN`
- `HOUSEKEEPER_CFN_EXECUTION_ROLE_ARN`
- `HOUSEKEEPER_API_CERTIFICATE_ARN`
- `HOUSEKEEPER_API_DOMAIN_NAME`

Add `HOUSEKEEPER_SMOKE_ACCESS_TOKEN` as a protected environment secret. It is
used only as an in-memory bearer token by the authenticated smoke check and is
never printed or uploaded.

Before the first protected run, an account administrator must bootstrap CDK in
`af-south-1` and perform the one-time deployment that creates the OIDC and
CloudFormation execution roles. The protected role then assumes the CDK
bootstrap roles and passes the environment's CloudFormation execution role:

```powershell
$env:HOUSEKEEPER_ENVIRONMENT = "shared-development"
$env:HOUSEKEEPER_AWS_REGION = "af-south-1"
$env:HOUSEKEEPER_AWS_ACCOUNT = "<account-id>"
cdk bootstrap aws://<account-id>/af-south-1
```

Review the first `cdk deploy --all` from a trusted administrator session and
record its `GitHubDeploymentRoleArn` and `CloudFormationExecutionRoleArn` as
the protected environment variables. Subsequent deployments use GitHub OIDC;
no long-lived AWS access key is required.

The workflow provisions the stacks with the ECS API service at zero desired
tasks, pushes an immutable image by digest, registers the image and Cognito
callback configuration while keeping the service stopped, runs `--migrate` as
a one-shot task with its separate task role, starts the API service, publishes
the PWA, invalidates CloudFront, and fails on readiness or authenticated smoke
failure. The ACM certificate must cover the configured API domain, and DNS for
that domain must point to the ALB before the smoke step.

## API image and migrations

The application stack points at the immutable `bootstrap` tag until the first
API image is published. CI builds the non-root image from
`deploy/containers/HouseKeeper.Api.Dockerfile`, pushes an immutable commit tag
and digest to ECR, and updates the service by digest.

Database migrations run as a separate, one-shot ECS task using a migration task
role. The task is launched by the protected deployment workflow, uses the RDS
secret through ECS secret injection, runs `dotnet HouseKeeper.Api.dll
--migrate`, and must complete successfully before the API service is started.
Normal API startup never changes the production schema.

No AWS credentials, access tokens, database passwords, or object contents are
placed in images, browser assets, logs, or CloudFormation outputs.

`IdentityStack` emits the user-pool issuer, hosted-login authority, and public
web client ID as deployment outputs. The API consumes the issuer and
public app client ID through ECS environment configuration. Cognito access
tokens carry that value in the `client_id` claim, which the API validates
alongside the `token_use=access` claim. The PWA
consumes only the hosted-login authority, public client ID, scopes, callback
URL, logout URL, and API base URL. A client secret is neither generated nor
required.

The API CORS allow-list is derived from the configured Cognito callback URL
origins and passed to the ECS task. This keeps the browser/API boundary aligned
with each deployed PWA origin rather than relying on localhost defaults.

After deploying shared development, validate the human journey with a
disposable Cognito user: create the account, verify the email, sign in through
managed login, create a household, sign out, and sign in again. Confirm that
removing the membership denies household access even though Cognito
authentication still succeeds.

## Teardown and recovery

Shared development is intentionally disposable. RDS uses a snapshot removal
policy, S3 buckets and non-production logs are deleted by CDK, and production
deletion protection is enabled. To tear down the shared environment after
confirming that no synthetic data is needed, review `cdk diff` and run:

```powershell
$env:HOUSEKEEPER_ENVIRONMENT = "shared-development"
$env:HOUSEKEEPER_AWS_REGION = "af-south-1"
cd deploy/aws
cdk destroy --all --force
```

Retain the final CloudFormation events, migration log, smoke result, and RDS
snapshot identifier as deployment evidence. Do not store PWA files, database
passwords, Cognito passwords, access tokens, or household data in those
artifacts.
