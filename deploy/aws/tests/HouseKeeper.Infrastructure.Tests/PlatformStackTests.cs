using Amazon.CDK;
using Amazon.CDK.Assertions;

using HouseKeeper.Infrastructure.Configuration;
using HouseKeeper.Infrastructure.Stacks;

using Xunit;

namespace HouseKeeper.Infrastructure.Tests;

public sealed class PlatformStackTests
{
    private static readonly string[] AuthorizationCodeFlow = ["code"];

    [Fact]
    public void StorageStackKeepsPwaAndAttachmentsPrivate()
    {
        App app = new();
        StorageStack stack = new(app, "Storage", StackProps(), Configuration());
        Template template = Template.FromStack(stack);

        template.ResourceCountIs("AWS::S3::Bucket", 2);
        template.HasResource("AWS::CloudFront::OriginAccessControl", new Dictionary<string, object>());
        template.HasResource("AWS::GuardDuty::MalwareProtectionPlan", new Dictionary<string, object>());
        template.HasResource("AWS::S3::BucketPolicy", new Dictionary<string, object>());
    }

    [Fact]
    public void ApplicationStackUsesImmutableScannedImagesAndPrivateTasks()
    {
        App app = new();
        PlatformConfiguration configuration = Configuration();
        NetworkStack network = new(app, "Network", StackProps(), configuration);
        DataStack data = new(app, "Data", StackProps(), configuration, network);
        IdentityStack identity = new(app, "Identity", StackProps(), configuration);
        StorageStack storage = new(app, "Storage", StackProps(), configuration);
        ApplicationStack stack = new(
            app,
            "Application",
            StackProps(),
            configuration,
            network,
            data,
            storage,
            identity);
        Template template = Template.FromStack(stack);

        template.HasResourceProperties(
            "AWS::ECR::Repository",
            new Dictionary<string, object>
            {
                ["ImageTagMutability"] = "IMMUTABLE",
                ["ImageScanningConfiguration"] = new Dictionary<string, object>
                {
                    ["ScanOnPush"] = true
                }
            });
        template.HasResource("AWS::ECS::Service", new Dictionary<string, object>());
        template.HasResource("AWS::ElasticLoadBalancingV2::LoadBalancer", new Dictionary<string, object>());
    }

    [Fact]
    public void ApplicationStackDefersServiceTasksUntilAnImmutableImageExists()
    {
        App app = new();
        PlatformConfiguration configuration = Configuration();
        NetworkStack network = new(app, "Network", StackProps(), configuration);
        DataStack data = new(app, "Data", StackProps(), configuration, network);
        IdentityStack identity = new(app, "Identity", StackProps(), configuration);
        StorageStack storage = new(app, "Storage", StackProps(), configuration);
        ApplicationStack stack = new(
            app,
            "Application",
            StackProps(),
            configuration,
            network,
            data,
            storage,
            identity);
        Template template = Template.FromStack(stack);

        template.HasResourceProperties(
            "AWS::ECS::Service",
            new Dictionary<string, object>
            {
                ["DesiredCount"] = 0
            });
        template.ResourceCountIs("AWS::ECS::TaskDefinition", 2);
        template.HasResource("AWS::IAM::Role", new Dictionary<string, object>());
    }

    [Fact]
    public void DeliveryStackRestrictsOidcTrustToRepositoryBranch()
    {
        App app = new();
        PlatformConfiguration configuration = Configuration();
        NetworkStack network = new(app, "Network", StackProps(), configuration);
        DataStack data = new(app, "Data", StackProps(), configuration, network);
        IdentityStack identity = new(app, "Identity", StackProps(), configuration);
        StorageStack storage = new(app, "Storage", StackProps(), configuration);
        ApplicationStack application = new(
            app,
            "Application",
            StackProps(),
            configuration,
            network,
            data,
            storage,
            identity);
        DeliveryStack stack = new(
            app,
            "Delivery",
            StackProps(),
            configuration,
            application,
            storage);
        Template template = Template.FromStack(stack);

        template.HasResourceProperties(
            "AWS::IAM::Role",
            new Dictionary<string, object>
            {
                ["RoleName"] = "housekeeper-test-github-deploy",
                ["AssumeRolePolicyDocument"] = new Dictionary<string, object>
                {
                    ["Statement"] = Match.ArrayWith(
                        new object[]
                        {
                            Match.ObjectLike(
                                new Dictionary<string, object>
                                {
                                    ["Condition"] = Match.ObjectLike(
                                        new Dictionary<string, object>
                                        {
                                            ["StringLike"] = Match.ObjectLike(
                                                new Dictionary<string, object>
                                                {
                                                    ["token.actions.githubusercontent.com:sub"] =
                                                        "repo:JohannesMogashoa/housekeeper:ref:refs/heads/master"
                                                })
                                        })
                                })
                        })
                }
            });
    }

    [Fact]
    public void ObservabilityStackCreatesBudgetAndEnvironmentResourceGroup()
    {
        App app = new();
        PlatformConfiguration configuration = Configuration();
        NetworkStack network = new(app, "Network", StackProps(), configuration);
        DataStack data = new(app, "Data", StackProps(), configuration, network);
        IdentityStack identity = new(app, "Identity", StackProps(), configuration);
        StorageStack storage = new(app, "Storage", StackProps(), configuration);
        ApplicationStack application = new(
            app,
            "Application",
            StackProps(),
            configuration,
            network,
            data,
            storage,
            identity);
        ObservabilityStack stack = new(
            app,
            "Observability",
            StackProps(),
            configuration,
            application,
            data);
        Template template = Template.FromStack(stack);

        template.HasResource("AWS::Budgets::Budget", new Dictionary<string, object>());
        template.HasResource("AWS::ResourceGroups::Group", new Dictionary<string, object>());
    }

    [Fact]
    public void IdentityStackUsesAuthorizationCodeGrantWithoutClientSecret()
    {
        App app = new();
        IdentityStack stack = new(app, "Identity", StackProps(), Configuration());
        Template template = Template.FromStack(stack);

        template.HasResourceProperties(
            "AWS::Cognito::UserPoolClient",
            new Dictionary<string, object>
            {
                ["GenerateSecret"] = false,
                ["AllowedOAuthFlows"] = AuthorizationCodeFlow,
                ["AllowedOAuthFlowsUserPoolClient"] = true
            });
    }

    private static PlatformConfiguration Configuration() => new()
    {
        EnvironmentName = "test",
        Account = "123456789012",
        Region = PlatformConfiguration.DefaultRegion,
        GitHubRepository = PlatformConfiguration.DefaultGitHubRepository,
        GitHubBranch = "master",
        CognitoDomainPrefix = "housekeeper-test-domain",
        CallbackUrls = ["http://localhost:5136/authentication/login-callback"],
        LogoutUrls = ["http://localhost:5136/authentication/logout-callback"]
    };

    private static StackProps StackProps() => new()
    {
        Env = new Amazon.CDK.Environment
        {
            Account = "123456789012",
            Region = PlatformConfiguration.DefaultRegion
        }
    };
}
