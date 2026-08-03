using Amazon.CDK;
using Amazon.CDK.AWS.IAM;
using Cdklabs.CdkNag;

using Constructs;

using HouseKeeper.Infrastructure.Configuration;

namespace HouseKeeper.Infrastructure.Stacks;

public sealed class DeliveryStack : Stack
{
    public DeliveryStack(
        Construct scope,
        string id,
        StackProps props,
        PlatformConfiguration configuration,
        ApplicationStack application,
        StorageStack storage)
        : base(scope, id, props)
    {
        GitHubOidcProvider = new OpenIdConnectProvider(
            this,
            "GitHubOidcProvider",
            new OpenIdConnectProviderProps
            {
                Url = "https://token.actions.githubusercontent.com",
                ClientIds = ["sts.amazonaws.com"]
            });

        DeploymentRole = new Role(
            this,
            "GitHubDeploymentRole",
            new RoleProps
            {
                RoleName = $"housekeeper-{configuration.EnvironmentName}-github-deploy",
                Description = "Protected HouseKeeper GitHub Actions deployment role. Attach only environment-scoped CDK deployment permissions.",
                MaxSessionDuration = Duration.Hours(1),
                AssumedBy = new OpenIdConnectPrincipal(
                    GitHubOidcProvider,
                    new Dictionary<string, object>
                    {
                        ["StringEquals"] = new Dictionary<string, object>
                        {
                            ["token.actions.githubusercontent.com:aud"] = "sts.amazonaws.com"
                        },
                        ["StringLike"] = new Dictionary<string, object>
                        {
                            ["token.actions.githubusercontent.com:sub"] =
                                $"repo:{configuration.GitHubRepository}:ref:refs/heads/{configuration.GitHubBranch}"
                        }
                    })
            });

        DeploymentRole.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps
                {
                    Effect = Effect.ALLOW,
                    Actions =
                    [
                        "cloudformation:DescribeStacks",
                        "cloudformation:DescribeStackEvents",
                        "cloudformation:GetTemplate"
                    ],
                    Resources = ["*"]
                }));

        DeploymentRole.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps
                {
                    Effect = Effect.ALLOW,
                    Actions =
                    [
                        "cloudformation:CreateChangeSet",
                        "cloudformation:CreateStack",
                        "cloudformation:DeleteChangeSet",
                        "cloudformation:DeleteStack",
                        "cloudformation:DescribeChangeSet",
                        "cloudformation:DescribeStackEvents",
                        "cloudformation:DescribeStacks",
                        "cloudformation:ExecuteChangeSet",
                        "cloudformation:GetTemplate",
                        "cloudformation:UpdateStack",
                        "cloudformation:UpdateTerminationProtection"
                    ],
                    Resources = ["*"]
                }));

        DeploymentRole.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps
                {
                    Effect = Effect.ALLOW,
                    Actions = ["ecr:GetAuthorizationToken"],
                    Resources = ["*"]
                }));
        DeploymentRole.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps
                {
                    Effect = Effect.ALLOW,
                    Actions =
                    [
                        "ecr:BatchCheckLayerAvailability",
                        "ecr:CompleteLayerUpload",
                        "ecr:DescribeImages",
                        "ecr:InitiateLayerUpload",
                        "ecr:PutImage",
                        "ecr:UploadLayerPart"
                    ],
                    Resources = [application.ApiRepository.RepositoryArn]
                }));
        DeploymentRole.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps
                {
                    Effect = Effect.ALLOW,
                    Actions = ["s3:DeleteObject", "s3:GetObject", "s3:ListBucket", "s3:PutObject"],
                    Resources = [storage.PwaBucket.BucketArn, storage.PwaBucket.ArnForObjects("*")]
                }));
        DeploymentRole.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps
                {
                    Effect = Effect.ALLOW,
                    Actions = ["cloudfront:CreateInvalidation", "cloudfront:GetDistribution"],
                    Resources = [storage.PwaDistribution.DistributionArn]
                }));
        DeploymentRole.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps
                {
                    Effect = Effect.ALLOW,
                    Actions =
                    [
                        "ecs:DescribeClusters"
                    ],
                    Resources = [application.Cluster.ClusterArn]
                }));
        DeploymentRole.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps
                {
                    Effect = Effect.ALLOW,
                    Actions = ["ecs:DescribeServices", "ecs:UpdateService"],
                    Resources = [application.ApiService.Service.ServiceArn]
                }));
        DeploymentRole.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps
                {
                    Effect = Effect.ALLOW,
                    Actions = ["ecs:DescribeTaskDefinition"],
                    Resources = ["*"]
                }));
        DeploymentRole.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps
                {
                    Effect = Effect.ALLOW,
                    Actions = ["ecs:RunTask"],
                    Resources = [application.MigrationTaskDefinition.TaskDefinitionArn],
                    Conditions = new Dictionary<string, object>
                    {
                        ["ArnEquals"] = new Dictionary<string, object>
                        {
                            ["ecs:cluster"] = application.Cluster.ClusterArn
                        }
                    }
                }));
        DeploymentRole.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps
                {
                    Effect = Effect.ALLOW,
                    Actions = ["ecs:DescribeTasks", "ecs:StopTask"],
                    Resources = ["*"],
                    Conditions = new Dictionary<string, object>
                    {
                        ["ArnEquals"] = new Dictionary<string, object>
                        {
                            ["ecs:cluster"] = application.Cluster.ClusterArn
                        }
                    }
                }));
        DeploymentRole.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps
                {
                    Effect = Effect.ALLOW,
                    Actions = ["iam:PassRole"],
                    Resources = [application.ExecutionRole.RoleArn, application.TaskRole.RoleArn, application.MigrationRole.RoleArn]
                }));

        CloudFormationExecutionRole = new Role(
            this,
            "CloudFormationExecutionRole",
            new RoleProps
            {
                RoleName = $"housekeeper-{configuration.EnvironmentName}-cfn-execution",
                Description = "HouseKeeper CloudFormation execution role for the isolated environment.",
                AssumedBy = new ServicePrincipal("cloudformation.amazonaws.com"),
                MaxSessionDuration = Duration.Hours(1)
            });
        CloudFormationExecutionRole.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps
                {
                    Effect = Effect.ALLOW,
                    Actions =
                    [
                        "acm:DescribeCertificate",
                        "budgets:*",
                        "cloudfront:*",
                        "cloudwatch:*",
                        "cognito-idp:*",
                        "ec2:*",
                        "ecr:*",
                        "ecs:*",
                        "elasticloadbalancing:*",
                        "guardduty:*",
                        "iam:*",
                        "logs:*",
                        "rds:*",
                        "resource-groups:*",
                        "s3:*",
                        "secretsmanager:*",
                        "ssm:*",
                        "xray:*"
                    ],
                    Resources = ["*"]
                }));
        DeploymentRole.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps
                {
                    Effect = Effect.ALLOW,
                    Actions = ["iam:PassRole"],
                    Resources = [CloudFormationExecutionRole.RoleArn]
                }));
        DeploymentRole.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps
                {
                    Effect = Effect.ALLOW,
                    Actions = ["sts:AssumeRole"],
                    Resources =
                    [
                        configuration.Account is null
                            ? "*"
                            : $"arn:{Aws.PARTITION}:iam::{configuration.Account}:role/cdk-hnb659fds-*"
                    ]
                }));

        _ = new CfnOutput(
            this,
            "GitHubDeploymentRoleArn",
            new CfnOutputProps { Value = DeploymentRole.RoleArn });
        _ = new CfnOutput(
            this,
            "CloudFormationExecutionRoleArn",
            new CfnOutputProps { Value = CloudFormationExecutionRole.RoleArn });

        Amazon.CDK.Tags.Of(this).Add("Application", "HouseKeeper");
        Amazon.CDK.Tags.Of(this).Add("Environment", configuration.EnvironmentName);
        Amazon.CDK.Tags.Of(this).Add("ManagedBy", "AWS-CDK");

        NagSuppressions.AddResourceSuppressions(
            DeploymentRole,
            new[]
            {
                new NagPackSuppression
                {
                    Id = "AwsSolutions-IAM5",
                    Reason = "The OIDC deployment role needs wildcard CloudFormation create/read scope and API token discovery; data-plane permissions are scoped to this environment's resources."
                }
            },
            true);
        NagSuppressions.AddResourceSuppressions(
            CloudFormationExecutionRole,
            new[]
            {
                new NagPackSuppression
                {
                    Id = "AwsSolutions-IAM5",
                    Reason = "CloudFormation creates the explicitly modeled resources across the isolated development environment; the role is trusted only by CloudFormation."
                }
            },
            true);
    }

    public OpenIdConnectProvider GitHubOidcProvider { get; }

    public Role DeploymentRole { get; }

    public Role CloudFormationExecutionRole { get; }
}
