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
