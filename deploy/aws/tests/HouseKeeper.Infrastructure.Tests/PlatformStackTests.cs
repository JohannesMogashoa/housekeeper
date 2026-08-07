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
    public void StorageStackOmitsGuardDutyWhenScanningIsDisabled()
    {
        App app = new();
        StorageStack stack = new(app, "Storage", StackProps(), Configuration() with
        {
            EnableGuardDuty = false
        });
        Template template = Template.FromStack(stack);

        template.ResourceCountIs("AWS::S3::Bucket", 2);
        template.ResourceCountIs("AWS::GuardDuty::Detector", 0);
        template.ResourceCountIs("AWS::GuardDuty::MalwareProtectionPlan", 0);
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
    public void DevelopmentDataStackDisablesAutomatedBackupRetentionForFreeTier()
    {
        App app = new();
        PlatformConfiguration configuration = Configuration();
        NetworkStack network = new(app, "Network", StackProps(), configuration);
        DataStack stack = new(app, "Data", StackProps(), configuration, network);
        Template template = Template.FromStack(stack);

        template.HasResourceProperties(
            "AWS::RDS::DBInstance",
            new Dictionary<string, object>
            {
                ["BackupRetentionPeriod"] = 0,
                ["DeletionProtection"] = false,
                ["MultiAZ"] = false
            });
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
    public void DeliveryStackRestrictsOidcTrustToProtectedGitHubEnvironment()
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
        GitHubOidcStack githubOidc = new(app, "GitHubOidc", StackProps());
        DeliveryStack stack = new(
            app,
            "Delivery",
            StackProps(),
            configuration,
            application,
            storage,
            githubOidc.Provider);
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
                                            ["StringEquals"] = Match.ObjectLike(
                                                new Dictionary<string, object>
                                                {
                                                    ["token.actions.githubusercontent.com:repository"] =
                                                        "JohannesMogashoa/housekeeper",
                                                    ["token.actions.githubusercontent.com:sub"] =
                                                        "repo:JohannesMogashoa/housekeeper:environment:test"
                                                })
                                        })
                                })
                        })
                }
            });
    }

    [Fact]
    public void ObservabilityStackCreatesEnvironmentResourceGroup()
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

        template.HasResource("AWS::ResourceGroups::Group", new Dictionary<string, object>());
    }

    [Fact]
    public void BudgetStackCreatesMonthlyAccountBudget()
    {
        App app = new();
        BudgetStack stack = new(
            app,
            "Budget",
            new StackProps
            {
                Env = new Amazon.CDK.Environment
                {
                    Account = "123456789012",
                    Region = "us-east-1"
                }
            },
            Configuration());
        Template template = Template.FromStack(stack);

        template.HasResource("AWS::Budgets::Budget", new Dictionary<string, object>());
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
        GitHubEnvironment = "test",
        CognitoDomainPrefix = "housekeeper-test-domain",
        CallbackUrls = ["http://localhost:5136/authentication/login-callback"],
        LogoutUrls = ["http://localhost:5136/authentication/logout-callback"],
        EnableGuardDuty = true,
        PwaCertificateArn = "arn:aws:acm:us-east-1:123456789012:certificate/test",
        PwaDomainName = "housekeeper-test.example.com"
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
