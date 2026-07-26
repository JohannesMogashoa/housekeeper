# HouseKeeper AWS infrastructure

This directory contains the C# AWS CDK application for issue #20. The default
target is `af-south-1`; environment-specific values are supplied through
`HOUSEKEEPER_*` variables or the standard CDK account context.

## Stacks

- `NetworkStack`: VPC, public load-balancer subnets, private application
  subnets, isolated database subnets, and security groups.
- `DataStack`: encrypted PostgreSQL RDS, generated Secrets Manager credentials,
  backups, deletion protection in production, and the migration endpoint
  parameter.
- `IdentityStack`: Cognito User Pool, authorization-code PKCE web client, and
  API scopes. Household membership and roles remain application-owned.
- `StorageStack`: private PWA and attachment buckets, CloudFront Origin Access
  Control, SigV4-compatible private object access, and GuardDuty Malware
  Protection for S3.
- `ApplicationStack`: immutable, scan-on-push ECR, non-public ECS Fargate API
  tasks, ALB health checks, ECS Exec, task roles, and deployment rollback.
- `DeliveryStack`: GitHub Actions OIDC provider and branch-scoped deployment
  role. The role is intentionally narrow and should be expanded only with the
  exact CloudFormation and bootstrap permissions required by the deployment
  workflow.
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

## API image and migrations

The application stack points at the immutable `bootstrap` tag until the first
API image is published. CI builds the non-root image from
`deploy/containers/HouseKeeper.Api.Dockerfile`, pushes an immutable commit tag
and digest to ECR, and updates the service by digest.

Database migrations run as a separate, one-shot ECS task using a migration task
role. The task is launched by the protected deployment workflow, uses the RDS
secret through ECS secret injection, runs the approved EF migration command,
and must complete successfully before the API service is updated. Normal API
startup never changes the production schema.

No AWS credentials, access tokens, database passwords, or object contents are
placed in images, browser assets, logs, or CloudFormation outputs.
