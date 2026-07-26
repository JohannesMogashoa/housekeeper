using Amazon.CDK;
using Amazon.CDK.AWS.IAM;

using Constructs;

using HouseKeeper.Infrastructure.Configuration;

namespace HouseKeeper.Infrastructure.Stacks;

public sealed class DeliveryStack : Stack
{
    public DeliveryStack(
        Construct scope,
        string id,
        StackProps props,
        PlatformConfiguration configuration)
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

        _ = new CfnOutput(
            this,
            "GitHubDeploymentRoleArn",
            new CfnOutputProps { Value = DeploymentRole.RoleArn });

        Amazon.CDK.Tags.Of(this).Add("Application", "HouseKeeper");
        Amazon.CDK.Tags.Of(this).Add("Environment", configuration.EnvironmentName);
        Amazon.CDK.Tags.Of(this).Add("ManagedBy", "AWS-CDK");
    }

    public OpenIdConnectProvider GitHubOidcProvider { get; }

    public Role DeploymentRole { get; }
}
