# GitHub project and deployment setup

This is the setup guide for the HouseKeeper repository. It combines the
GitHub branch, ruleset, Codex, AWS OIDC, environment-variable, and secret
configuration required before using the release workflow.

The normal development loop is cloud-free. AWS is used only by the protected
`shared-development` pre-production path and the protected `production` path.

## 1. Understand the lifecycle

HouseKeeper moves through these refs:

```text
feature/* -> development -> rc/vX.Y.Z -> release/vX.Y.Z -> master -> vX.Y.Z
```

| Ref or event | Result |
|---|---|
| Pull request to `development` | Build, tests, migration-backed API smoke, and Codex review request. No publishing, artifacts, AWS credentials, or deployment. |
| Push to `development` | Full validation only. No AWS credentials or deployment. |
| `rc/vX.Y.Z` tag | Verifies that the tag is reachable from `development`, creates `release/vX.Y.Z`, and opens one promotion PR to `master`. |
| Pull request to `release/**` or `master` | Full validation only. No AWS credentials or deployment. |
| Release pull request | Application-only changes skip AWS. Infrastructure, migration, container, workflow, and deployment-script changes require an explicitly enabled shared-development pre-production run. |
| Merged `release/vX.Y.Z` pull request | Deploys the exact successful candidate to protected production. A successful deployment creates `vX.Y.Z` and the GitHub Release. |
| Failed production deployment | Creates no final release tag. Retry or rollback uses the release runbook. |

The workflow job names used by branch rulesets are normally displayed as:

```text
Development pull-request validation / Build, test, and API smoke
Full pull-request validation / Build, test, migrate, and smoke
```

GitHub can slightly change how reusable-workflow checks are displayed. Select
the exact check name shown after the first successful run rather than guessing
from a similarly named workflow.

## 2. Treat documentation as a solution item

Every work item and pull request is complete only when its applicable solution
items are complete:

- implementation or configuration change;
- tests and validation evidence;
- documentation, runbook, diagram, migration note, or operational guidance;
- deployment, rollback, cost, and recovery notes when the change affects them.

Documentation is part of the solution, not a follow-up task. If no document
requires changing, record why in the pull request instead of silently omitting
the item. The pull-request template contains the concise author checklist.

## 3. Prepare local and GitHub access

Install or authenticate the following tools:

- Git
- GitHub CLI (`gh`)
- AWS CLI
- .NET 10 SDK
- AWS CDK CLI
- Docker, for local validation

Authenticate GitHub CLI and confirm the repository:

```powershell
gh auth login
gh repo view JohannesMogashoa/housekeeper
```

The repository's default production branch is `master`. The integration
branch is `development`. If `development` already exists on GitHub, check it
out locally:

```powershell
git fetch origin
git switch --create development --track origin/development
```

If it does not exist on GitHub, create it from the intended starting commit
and publish it:

```powershell
git switch --create development
git push --set-upstream origin development
```

## 4. Configure branch rulesets

Open:

```text
Repository -> Settings -> Rules -> Rulesets -> New branch ruleset
```

Create the following rulesets. Keep them active and do not allow force pushes
or deletion.

### Development ruleset

Target branch:

```text
development
```

Enable:

- Require a pull request before merging.
- Require the `Development pull-request validation / Build, test, and API smoke` check.
- Require conversation resolution.
- Block force pushes.
- Block branch deletion.

For a team with another available reviewer, require one approving human
review. If you are temporarily the sole contributor, require the pull request
and successful check but use zero required approvals on `development` until a
second reviewer is available. Do not use this exception for production.

Do not require publishing, Playwright, CDK, AWS deployment, or production
checks on pull requests to `development`. The development PR workflow is
deliberately limited to build, tests, migration-backed API smoke, and review.

### Release and production ruleset

Target both:

```text
release/**
master
```

Enable:

- Require a pull request before merging.
- Require the `Full pull-request validation / Build, test, migrate, and smoke` check.
- Require conversation resolution.
- Block force pushes.
- Block branch deletion.
- Require at least one human approval when a second reviewer is available.

For `master`, require one independent human approval before merge. The
production environment approval is a separate deployment gate and must remain
enabled even when branch approval is complete.

Do not allow ordinary feature branches to merge directly into `master`.

### Tag rulesets

Create a tag ruleset covering:

```text
rc/v*.*.*
v*.*.*
```

Prevent tag deletion and updates. Restrict tag creation to the release
maintainer and the reviewed release automation. If the final-tag workflow is
blocked, add only the GitHub Actions identity or release automation required
for these exact tag patterns to the ruleset bypass list. Do not broadly bypass
the ruleset for all contributors.

The `rc/vX.Y.Z` tag must point to a commit reachable from `development`. The
promotion workflow rejects tags that do not satisfy this condition.

## 5. Configure GitHub Actions settings

Open:

```text
Repository -> Settings -> Actions -> General
```

Use these settings:

- Allow only actions approved for this repository or organization where practical.
- Keep the default workflow permission at read-only.
- Do not add AWS credentials, deployment roles, or production secrets to pull-request workflows.
- Allow the workflows in this repository to use the `GITHUB_TOKEN` permissions explicitly declared in their YAML files.

The important permission boundary is:

- Development and release validation: repository contents read only.
- Codex request workflow: contents read and pull-request write only.
- Protected deployment: contents/actions read and `id-token: write`.
- Final tag and release publication: contents write only in the post-deployment job.

## 6. Connect and configure Codex review

Codex review is separate from CI and human approval.

1. Open Codex and connect the GitHub account or organization that owns `JohannesMogashoa/housekeeper`.
2. Authorize repository access for HouseKeeper.
3. Create or select the HouseKeeper Codex environment.
4. Enable automatic code review for this repository if automatic reviews are desired.
5. Choose whether reviews run when a draft becomes ready, after every push, or both.
6. Open a small test pull request targeting `development`.
7. Confirm that the repository workflow posts one Codex request comment.
8. Confirm that Codex reads the repository `AGENTS.md` review contract.

The repository workflow is `.github/workflows/codex-review-request.yml`. It
runs on `pull_request_target`, has only `contents: read` and
`pull-requests: write`, never checks out pull-request code, and never runs
untrusted pull-request code.

The workflow asks Codex to:

- review the pull request;
- generate a concise description; and
- place the description between the `housekeeper-codex-description` markers in the pull request body when the connected integration supports body write-back.

If the Codex integration returns the description without updating the body,
copy the generated text into that marked section manually. Do not treat the
comment as a synthetic passing check. Resolve or explicitly disposition
material Codex findings, then obtain the required human approval.

The pull-request template is tracker-neutral. Link a GitHub issue, Notion page,
Linear task, another planning item, or state that no external item exists. The
author still records the target branch, infrastructure impact, pre-production
decision, validation run, documentation status, and residual operational risk.

For focused reviews, comment on the pull request with a request such as:

```text
@codex review for household authorization, migration safety, deployment permissions, and restart recovery
```

See the [Codex pull-request review guide](pr-review-agent.md) for the review
contract and finding priorities.

## 7. Create protected GitHub environments

Open:

```text
Repository -> Settings -> Environments
```

Create these environments with these exact names:

```text
shared-development
production
```

Configure deployment branch restrictions:

| Environment | Allowed deployment refs |
|---|---|
| `shared-development` | `release/**` for release pre-production and exact candidate deployments |
| `production` | `master` only |

Configure required reviewers. Require an independent reviewer for
`production`. For `shared-development`, use a reviewer when the team has one;
the environment remains the deliberate gate for infrastructure-impacting
pre-production changes.

Dispatch `release-preproduction.yml` and `deploy-development.yml` from the
corresponding `release/vX.Y.Z` ref so the environment branch restriction and
the exact candidate source are aligned.

Enable the environment option that prevents the person who started a workflow
from approving their own deployment when the team size makes that practical.

Do not put environment secrets in repository-wide secrets unless a workflow
truly needs the same secret in every environment. Keep shared-development and
production values separate.

## 8. Choose the AWS account and region

Use a separate AWS account for shared-development and production where
possible. The CDK currently creates fixed CloudFormation stack names such as
`HouseKeeperApplication` and `HouseKeeperData`; using both environments in the
same account and region can make them collide.

The supported region is:

```text
af-south-1
```

Authenticate the AWS CLI with an administrator profile for the environment
being configured:

```powershell
$awsProfile = "housekeeper-shared"
$awsRegion = "af-south-1"
$env:AWS_PROFILE = $awsProfile

aws sts get-caller-identity `
  --profile $awsProfile `
  --query Account `
  --output text
```

Record the returned account ID as `HOUSEKEEPER_AWS_ACCOUNT`. Repeat the
process with a different profile and account for production.

Do not save AWS access keys in GitHub. GitHub Actions obtains short-lived AWS
credentials through OIDC.

## 9. Create the AWS sender identity

Household invitations use Amazon SES when the protected environment is
configured for SES delivery.

Choose a verified sender, for example:

```powershell
$fromAddress = "invitations@example.com"
```

Create an SES email identity:

```powershell
aws sesv2 create-email-identity `
  --email-identity $fromAddress `
  --profile $awsProfile `
  --region $awsRegion
```

Click the verification email, then check the result:

```powershell
aws sesv2 get-email-identity `
  --email-identity $fromAddress `
  --profile $awsProfile `
  --region $awsRegion `
  --query VerifiedForSendingStatus `
  --output text
```

The result must be `True`. For a domain identity, publish the DKIM records
provided by SES.

Save the sender as the GitHub environment variable:

```text
HOUSEKEEPER_INVITATION_FROM_ADDRESS
```

Do this independently in each AWS account if the environments are separate.

## 10. Request and validate the API certificate

Choose the public API hostname for the environment:

```powershell
$apiDomain = "api-shared.example.com"
```

Request a DNS-validated ACM certificate in `af-south-1`:

```powershell
aws acm request-certificate `
  --domain-name $apiDomain `
  --validation-method DNS `
  --profile $awsProfile `
  --region $awsRegion
```

Add the ACM validation CNAME record to DNS and wait for the certificate to
reach `ISSUED`:

```powershell
aws acm list-certificates `
  --profile $awsProfile `
  --region $awsRegion `
  --certificate-statuses ISSUED `
  --query "CertificateSummaryList[?DomainName=='$apiDomain'].CertificateArn | [0]" `
  --output text
```

Save the returned ARN in a local variable and as the GitHub environment
variable:

```powershell
$certificateArn = aws acm list-certificates `
  --profile $awsProfile `
  --region $awsRegion `
  --certificate-statuses ISSUED `
  --query "CertificateSummaryList[?DomainName=='$apiDomain'].CertificateArn | [0]" `
  --output text
```

```text
HOUSEKEEPER_API_CERTIFICATE_ARN
```

The certificate must be in the same account and region as the API load
balancer.

## 11. Bootstrap CDK and create the OIDC roles

Perform this step once per AWS account as an AWS administrator. Set the
environment-specific values locally:

```powershell
$env:HOUSEKEEPER_ENVIRONMENT = "shared-development"
$env:HOUSEKEEPER_GITHUB_ENVIRONMENT = "shared-development"
$env:HOUSEKEEPER_AWS_REGION = "af-south-1"
$env:HOUSEKEEPER_AWS_ACCOUNT = "123456789012"
$env:HOUSEKEEPER_GITHUB_REPOSITORY = "JohannesMogashoa/housekeeper"
$env:HOUSEKEEPER_API_CERTIFICATE_ARN = "arn:aws:acm:af-south-1:123456789012:certificate/..."
$env:HOUSEKEEPER_API_DOMAIN_NAME = "api-shared.example.com"
$env:HOUSEKEEPER_INVITATION_FROM_ADDRESS = "invitations@example.com"
```

Bootstrap the account:

```powershell
cd deploy/aws
cdk bootstrap aws://123456789012/af-south-1
```

Review and synthesize before applying:

```powershell
cdk synth --strict
cdk diff
```

Perform the initial trusted deployment:

```powershell
cdk deploy --all --require-approval never
```

This creates the environment-scoped GitHub OIDC deployment role and the
CloudFormation execution role. Later GitHub Actions deployments must use OIDC
instead of the administrator profile.

Retrieve the role ARNs from `HouseKeeperDelivery`:

```powershell
$deployRoleArn = aws cloudformation describe-stacks `
  --stack-name HouseKeeperDelivery `
  --profile $awsProfile `
  --region $awsRegion `
  --query "Stacks[0].Outputs[?OutputKey=='GitHubDeploymentRoleArn'].OutputValue | [0]" `
  --output text

$cfnRoleArn = aws cloudformation describe-stacks `
  --stack-name HouseKeeperDelivery `
  --profile $awsProfile `
  --region $awsRegion `
  --query "Stacks[0].Outputs[?OutputKey=='CloudFormationExecutionRoleArn'].OutputValue | [0]" `
  --output text
```

Save these as GitHub environment variables, not secrets:

```text
HOUSEKEEPER_AWS_DEPLOY_ROLE_ARN
HOUSEKEEPER_CFN_EXECUTION_ROLE_ARN
```

The OIDC trust must identify the repository and the exact environment:

```text
aud = sts.amazonaws.com
repository = JohannesMogashoa/housekeeper
sub = repo:JohannesMogashoa/housekeeper:environment:<environment-name>
```

The environment name in the trust policy, GitHub environment, deployment
workflow input, and `HOUSEKEEPER_GITHUB_ENVIRONMENT` value must match.

## 12. Create the API DNS record

After the Application stack exists, retrieve the load balancer hostname:

```powershell
$loadBalancerDns = aws cloudformation describe-stacks `
  --stack-name HouseKeeperApplication `
  --profile $awsProfile `
  --region $awsRegion `
  --query "Stacks[0].Outputs[?OutputKey=='ApiLoadBalancerDnsName'].OutputValue | [0]" `
  --output text

$loadBalancerDns
```

Create the DNS record in your DNS provider:

```text
api-shared.example.com  CNAME  <load-balancer-dns-name>
```

Use the production API hostname and production load balancer for the
production account. The current CDK creates the load balancer but does not
create this DNS record.

## 13. Create the Cognito smoke-test identity

The protected deployment uses `HOUSEKEEPER_SMOKE_ACCESS_TOKEN` to call the API
smoke and persistence checks. It must be a real Cognito access token, not a
random value and not an ID token.

Retrieve the Cognito outputs:

```powershell
$userPoolId = aws cloudformation describe-stacks `
  --stack-name HouseKeeperIdentity `
  --profile $awsProfile `
  --region $awsRegion `
  --query "Stacks[0].Outputs[?OutputKey=='UserPoolId'].OutputValue | [0]" `
  --output text

$clientId = aws cloudformation describe-stacks `
  --stack-name HouseKeeperIdentity `
  --profile $awsProfile `
  --region $awsRegion `
  --query "Stacks[0].Outputs[?OutputKey=='WebClientId'].OutputValue | [0]" `
  --output text

$authority = aws cloudformation describe-stacks `
  --stack-name HouseKeeperIdentity `
  --profile $awsProfile `
  --region $awsRegion `
  --query "Stacks[0].Outputs[?OutputKey=='HostedUiAuthority'].OutputValue | [0]" `
  --output text
```

Create a disposable smoke user. Keep the password in a password manager and
never put it in source, workflow output, or an artifact:

```powershell
$smokeEmail = "housekeeper-smoke@example.com"
$smokePassword = "<strong-unique-password>"

aws cognito-idp sign-up `
  --client-id $clientId `
  --username $smokeEmail `
  --password $smokePassword `
  --user-attributes Name=email,Value=$smokeEmail `
  --profile $awsProfile `
  --region $awsRegion

aws cognito-idp admin-confirm-sign-up `
  --user-pool-id $userPoolId `
  --username $smokeEmail `
  --profile $awsProfile `
  --region $awsRegion
```

The current web client uses Cognito authorization-code flow with PKCE:

1. Open the Hosted UI authorization endpoint using `$authority`.
2. Request the registered PWA callback URL and the configured API scopes.
3. Sign in with the smoke user.
4. Capture the one-time callback authorization code locally.
5. Exchange the code at `$authority/oauth2/token` using the PKCE verifier.
6. Save only the returned `access_token` value for the environment secret.

Do not paste authorization codes, passwords, refresh tokens, or access tokens
into chat, source, logs, or artifacts. Cognito access tokens are short-lived,
so refresh the GitHub secret before it expires. A future improvement should
obtain a fresh smoke token inside a protected deployment step instead of
keeping a manually refreshed token.

## 14. Save GitHub environment variables and secrets

### Values to save as environment variables

Save the following values independently under both GitHub environments:

| Name | Value source | Secret? |
|---|---|---|
| `HOUSEKEEPER_AWS_ACCOUNT` | `aws sts get-caller-identity` | No |
| `HOUSEKEEPER_AWS_DEPLOY_ROLE_ARN` | `HouseKeeperDelivery` output `GitHubDeploymentRoleArn` | No |
| `HOUSEKEEPER_CFN_EXECUTION_ROLE_ARN` | `HouseKeeperDelivery` output `CloudFormationExecutionRoleArn` | No |
| `HOUSEKEEPER_API_CERTIFICATE_ARN` | Issued ACM certificate ARN | No |
| `HOUSEKEEPER_API_DOMAIN_NAME` | Chosen API DNS name | No |
| `HOUSEKEEPER_INVITATION_FROM_ADDRESS` | Verified SES sender | No |

The following values are generated or supplied by the workflows and should
not normally be entered manually as GitHub environment values:

| Name | Set by |
|---|---|
| `HOUSEKEEPER_ENVIRONMENT` | Deployment workflow input |
| `HOUSEKEEPER_GITHUB_ENVIRONMENT` | Deployment workflow input |
| `HOUSEKEEPER_AWS_REGION` | Repository deployment workflow (`af-south-1`) |
| `HOUSEKEEPER_GITHUB_REPOSITORY` | GitHub context |
| `HOUSEKEEPER_API_IMAGE_URI` | Candidate image promotion |
| `HOUSEKEEPER_API_DESIRED_COUNT` | Deployment workflow |
| `HOUSEKEEPER_COGNITO_CALLBACK_URLS` | Deployment workflow after resolving the PWA domain |
| `HOUSEKEEPER_COGNITO_LOGOUT_URLS` | Deployment workflow after resolving the PWA domain |

The CDK creates the database secret in AWS Secrets Manager. Do not copy the
database username or password into GitHub.

### Value to save as an environment secret

Save this secret separately under `shared-development` and `production`:

```text
HOUSEKEEPER_SMOKE_ACCESS_TOKEN
```

Using GitHub CLI:

```powershell
$repo = "JohannesMogashoa/housekeeper"
$accessToken = "<fresh-cognito-access-token>"

gh variable set HOUSEKEEPER_AWS_ACCOUNT `
  --env shared-development --repo $repo --body "123456789012"

gh variable set HOUSEKEEPER_AWS_DEPLOY_ROLE_ARN `
  --env shared-development --repo $repo --body $deployRoleArn

gh variable set HOUSEKEEPER_CFN_EXECUTION_ROLE_ARN `
  --env shared-development --repo $repo --body $cfnRoleArn

gh variable set HOUSEKEEPER_API_CERTIFICATE_ARN `
  --env shared-development --repo $repo --body $certificateArn

gh variable set HOUSEKEEPER_API_DOMAIN_NAME `
  --env shared-development --repo $repo --body $apiDomain

gh variable set HOUSEKEEPER_INVITATION_FROM_ADDRESS `
  --env shared-development --repo $repo --body $fromAddress

gh secret set HOUSEKEEPER_SMOKE_ACCESS_TOKEN `
  --env shared-development --repo $repo --body $accessToken
```

Repeat the commands with `--env production` and the production account,
roles, certificate, domain, sender, and access token. Do not echo the token.

The same values can be entered in the GitHub web interface under each
environment's **Variables** and **Secrets** sections. GitHub does not allow a
secret value to be read back. If it is uncertain or expired, generate a fresh
token and overwrite it.

## 15. Verify the configuration

List variable and secret names without revealing the secret value:

```powershell
gh variable list --env shared-development --repo $repo
gh secret list --env shared-development --repo $repo

gh variable list --env production --repo $repo
gh secret list --env production --repo $repo
```

Before a protected deployment, confirm:

- the GitHub environment name is exactly `shared-development` or `production`;
- the environment branch restriction permits the calling ref;
- the required reviewer is configured;
- the AWS account ID matches the intended environment;
- the deployment role trust contains the exact repository and environment;
- the ACM certificate is `ISSUED` in `af-south-1`;
- the SES sender is verified;
- the API DNS record resolves to the intended load balancer;
- the smoke secret is a current Cognito access token;
- the release candidate run ID and source SHA refer to the same successful artifact.

Run release validation before attempting a deployment. The deployment
workflow requires an exact successful release candidate artifact; it does not
build a replacement artifact during promotion.

## 16. Troubleshoot common failures

### `HOUSEKEEPER_* is required`

The value is missing from the selected GitHub environment or the workflow is
using a different environment name. Check the environment name, variable
spelling, and `environment: ${{ inputs.environment }}` job binding.

### `AssumeRoleWithWebIdentity` fails

Check the AWS account, deployment-role ARN, repository name, environment name,
OIDC audience, and GitHub environment branch restrictions. Do not replace the
OIDC flow with static AWS keys.

### Certificate or HTTPS readiness fails

Check that the certificate is issued in `af-south-1`, belongs to the target AWS
account, covers the exact API hostname, and that DNS resolves to the current
load balancer.

### SES invitation delivery fails

Check that `HOUSEKEEPER_INVITATION_FROM_ADDRESS` is passed to the deployment
workflow, the sender is verified in the target account and region, and the
application is configured for SES delivery.

### Smoke returns `401 Unauthorized`

The token is probably expired, an ID token was saved instead of an access
token, or the token belongs to the other environment's Cognito client. Obtain
a fresh access token from the correct environment and replace the GitHub
secret.

### Migration fails

Leave the service stopped, retain the migration task diagnostics, fix the
forward migration in a new candidate, and retry only after review. Never use a
destructive EF down-migration as an automatic rollback.

### Deployment fails after the application is updated

Inspect the deployment artifact, CDK diff, CloudFormation/ECS events, image
digest, readiness result, smoke result, and restart/persistence evidence. Use
the previous immutable image digest or the approved database restore procedure
as appropriate. Do not create the final release tag manually before production
evidence is complete.

## Related documentation

- [Release promotion runbook](release-promotion.md) - day-to-day promotion, candidate evidence, rollback, and recovery
- [Local development guide](local-development.md) - cloud-free development and local validation
- [Codex pull-request review](pr-review-agent.md) - review contract and Codex behavior
- [AWS deployment foundation](../../deploy/aws/README.md) - CDK stacks, artifact promotion, migration order, and cost boundaries
- [Root README](../../README.md) - repository overview and workflow summary
- [GitHub rulesets](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/about-rulesets)
- [GitHub OIDC with AWS](https://docs.github.com/en/actions/how-tos/secure-your-work/security-harden-deployments/oidc-in-aws)
- [AWS ACM certificates](https://docs.aws.amazon.com/cli/latest/reference/acm/list-certificates.html)
- [Amazon SES identities](https://docs.aws.amazon.com/cli/latest/reference/sesv2/create-email-identity.html)
- [Amazon Cognito token endpoint and PKCE](https://docs.aws.amazon.com/cognito/latest/developerguide/token-endpoint.html)
