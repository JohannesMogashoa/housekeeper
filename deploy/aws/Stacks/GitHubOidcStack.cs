using Amazon.CDK;
using Amazon.CDK.AWS.IAM;

using Constructs;

namespace HouseKeeper.Infrastructure.Stacks;

public sealed class GitHubOidcStack : Stack
{
    public GitHubOidcStack(
        Construct scope,
        string id,
        StackProps props)
        : base(scope, id, props)
    {
        Provider = new OpenIdConnectProvider(
            this,
            "GitHubOidcProvider",
            new OpenIdConnectProviderProps
            {
                Url = "https://token.actions.githubusercontent.com",
                ClientIds = ["sts.amazonaws.com"]
            });

        Amazon.CDK.Tags.Of(this).Add("Application", "HouseKeeper");
        Amazon.CDK.Tags.Of(this).Add("ManagedBy", "AWS-CDK");
        Amazon.CDK.Tags.Of(this).Add("Purpose", "GitHubActionsOIDC");
    }

    public OpenIdConnectProvider Provider { get; }
}
