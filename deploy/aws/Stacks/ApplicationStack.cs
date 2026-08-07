using Amazon.CDK;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Logs;
using Cdklabs.CdkNag;

using Constructs;

using HouseKeeper.Infrastructure.Configuration;

using EcsSecret = Amazon.CDK.AWS.ECS.Secret;

namespace HouseKeeper.Infrastructure.Stacks;

public sealed class ApplicationStack : Stack
{
    public ApplicationStack(
        Construct scope,
        string id,
        StackProps props,
        PlatformConfiguration configuration,
        NetworkStack network,
        DataStack data,
        StorageStack storage,
        IdentityStack identity)
        : base(scope, id, props)
    {
        ApiSecurityGroup = new SecurityGroup(
            this,
            "ApiSecurityGroup",
            new SecurityGroupProps
            {
                Vpc = network.Vpc,
                Description = "HouseKeeper API tasks accept traffic only from the load balancer.",
                AllowAllOutbound = true
            });

        MigrationSecurityGroup = new SecurityGroup(
            this,
            "MigrationSecurityGroup",
            new SecurityGroupProps
            {
                Vpc = network.Vpc,
                Description = "HouseKeeper migration tasks access PostgreSQL through a separate security group.",
                AllowAllOutbound = true
            });

        ApiRepository = new Repository(
            this,
            "ApiRepository",
            new RepositoryProps
            {
                RepositoryName = $"housekeeper/{configuration.EnvironmentName}/api",
                ImageScanOnPush = true,
                ImageTagMutability = TagMutability.IMMUTABLE,
                LifecycleRules =
                [
                    new LifecycleRule
                    {
                        MaxImageCount = configuration.IsProduction ? 50 : 15
                    }
                ],
                RemovalPolicy = RemovalPolicy.RETAIN
            });

        Cluster = new Cluster(
            this,
            "Cluster",
            new ClusterProps
            {
                ClusterName = $"housekeeper-{configuration.EnvironmentName}",
                Vpc = network.Vpc,
                ContainerInsightsV2 = ContainerInsights.ENHANCED
            });

        TaskRole = new Role(
            this,
            "ApiTaskRole",
            new RoleProps
            {
                AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
                Description = "HouseKeeper API runtime permissions; no deployment or migration permissions."
            });

        ExecutionRole = new Role(
            this,
            "ApiExecutionRole",
            new RoleProps
            {
                AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
                Description = "HouseKeeper ECS image, log and launch-time secret retrieval permissions."
            });
        MigrationRole = new Role(
            this,
            "MigrationTaskRole",
            new RoleProps
            {
                AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
                Description = "HouseKeeper migration permissions; separate from the API runtime role."
            });

        ApiLogGroup = CreateLogGroup("ApiLogGroup", "api", configuration);
        MigrationLogGroup = CreateLogGroup("MigrationLogGroup", "migration", configuration);
        ApiRepository.GrantPull(ExecutionRole);
        ApiLogGroup.GrantWrite(ExecutionRole);
        if (configuration.InvitationFromAddress is { Length: > 0 } invitationFromAddress)
        {
            TaskRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
            {
                Actions = ["ses:SendEmail"],
                Effect = Effect.ALLOW,
                Resources =
                [
                    Arn.Format(
                        new ArnComponents
                        {
                            Service = "ses",
                            Resource = "identity",
                            ResourceName = invitationFromAddress
                        },
                        this)
                ]
            }));
        }

        Dictionary<string, string> taskEnvironment = new()
        {
            ["ASPNETCORE_HTTP_PORTS"] = "8080",
            ["HOUSEKEEPER_AWS_REGION"] = configuration.Region,
            ["Authentication__Mode"] = "Cognito",
            ["Authentication__Cognito__Authority"] = identity.Issuer,
            ["Authentication__Cognito__ClientId"] = identity.WebClient.UserPoolClientId,
            ["InvitationDelivery__Mode"] = configuration.InvitationFromAddress is null
                ? "Disabled"
                : "Ses",
            ["InvitationDelivery__FromAddress"] = configuration.InvitationFromAddress ?? string.Empty
        };
        string[] pwaOrigins = configuration.CallbackUrls
            .Select(static url => new Uri(url).GetLeftPart(UriPartial.Authority))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        for (int index = 0; index < pwaOrigins.Length; index++)
        {
            taskEnvironment[$"Cors__AllowedOrigins__{index}"] = pwaOrigins[index];
        }
        taskEnvironment["InvitationDelivery__WebBaseUrl"] = pwaOrigins[0];

        ContainerImage apiImage = configuration.ApiImageUri is null
            ? ContainerImage.FromEcrRepository(ApiRepository, "bootstrap")
            : ContainerImage.FromRegistry(configuration.ApiImageUri);

        ApiService = new ApplicationLoadBalancedFargateService(
            this,
            "ApiService",
            new ApplicationLoadBalancedFargateServiceProps
            {
                Cluster = Cluster,
                Cpu = 512,
                MemoryLimitMiB = 1024,
                DesiredCount = Math.Max(configuration.ApiDesiredCount, 1),
                PublicLoadBalancer = true,
                AssignPublicIp = false,
                SecurityGroups = [ApiSecurityGroup],
                TaskSubnets = new SubnetSelection
                {
                    SubnetType = SubnetType.PRIVATE_WITH_EGRESS
                },
                TaskImageOptions = new ApplicationLoadBalancedTaskImageOptions
                {
                    Image = apiImage,
                    ContainerName = "housekeeper-api",
                    ContainerPort = 8080,
                    EnableLogging = true,
                    ExecutionRole = ExecutionRole,
                    TaskRole = TaskRole,
                    Environment = taskEnvironment,
                    Secrets = new Dictionary<string, EcsSecret>
                    {
                        ["HOUSEKEEPER_DB_HOST"] = EcsSecret.FromSecretsManager(data.DatabaseSecret, "host"),
                        ["HOUSEKEEPER_DB_PORT"] = EcsSecret.FromSecretsManager(data.DatabaseSecret, "port"),
                        ["HOUSEKEEPER_DB_NAME"] = EcsSecret.FromSecretsManager(data.DatabaseSecret, "dbname"),
                        ["HOUSEKEEPER_DB_USERNAME"] = EcsSecret.FromSecretsManager(data.DatabaseSecret, "username"),
                        ["HOUSEKEEPER_DB_PASSWORD"] = EcsSecret.FromSecretsManager(data.DatabaseSecret, "password")
                    },
                    LogDriver = LogDrivers.AwsLogs(
                        new AwsLogDriverProps
                        {
                            LogGroup = ApiLogGroup,
                            StreamPrefix = "api"
                        })
                },
                Protocol = configuration.ApiCertificateArn is null
                    ? ApplicationProtocol.HTTP
                    : ApplicationProtocol.HTTPS,
                TargetProtocol = ApplicationProtocol.HTTP,
                ListenerPort = configuration.ApiCertificateArn is null ? 80 : 443,
                Certificate = configuration.ApiCertificateArn is null
                    ? null
                    : Certificate.FromCertificateArn(this, "ApiCertificate", configuration.ApiCertificateArn),
                RedirectHTTP = configuration.ApiCertificateArn is not null,
                HealthCheckGracePeriod = Duration.Seconds(60),
                CircuitBreaker = new DeploymentCircuitBreaker { Rollback = true },
                MinHealthyPercent = 100,
                MaxHealthyPercent = 200,
                EnableExecuteCommand = true
            });

        if ((configuration.ApiImageUri is null || configuration.ApiDesiredCount == 0) &&
            ApiService.Service.Node.DefaultChild is CfnService serviceResource)
        {
            // ECS accepts zero desired tasks, while the pattern constructor requires
            // a positive value. This makes the first infrastructure deployment safe
            // before the immutable API image has been pushed to ECR.
            serviceResource.AddPropertyOverride("DesiredCount", 0);
        }

        MigrationTaskDefinition = new FargateTaskDefinition(
            this,
            "MigrationTaskDefinition",
            new FargateTaskDefinitionProps
            {
                Cpu = 512,
                MemoryLimitMiB = 1024,
                ExecutionRole = ExecutionRole,
                TaskRole = MigrationRole
            });

        _ = MigrationTaskDefinition.AddContainer(
            "MigrationContainer",
            new ContainerDefinitionOptions
            {
                Image = apiImage,
                ContainerName = "housekeeper-migration",
                Command = ["--migrate"],
                Environment = taskEnvironment,
                Secrets = new Dictionary<string, EcsSecret>
                {
                    ["HOUSEKEEPER_DB_HOST"] = EcsSecret.FromSecretsManager(data.DatabaseSecret, "host"),
                    ["HOUSEKEEPER_DB_PORT"] = EcsSecret.FromSecretsManager(data.DatabaseSecret, "port"),
                    ["HOUSEKEEPER_DB_NAME"] = EcsSecret.FromSecretsManager(data.DatabaseSecret, "dbname"),
                    ["HOUSEKEEPER_DB_USERNAME"] = EcsSecret.FromSecretsManager(data.DatabaseSecret, "username"),
                    ["HOUSEKEEPER_DB_PASSWORD"] = EcsSecret.FromSecretsManager(data.DatabaseSecret, "password")
                },
                Logging = LogDrivers.AwsLogs(
                    new AwsLogDriverProps
                    {
                        LogGroup = MigrationLogGroup,
                        StreamPrefix = "migration"
                    })
            });

        ApiService.TargetGroup.ConfigureHealthCheck(
            new Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck
            {
                Path = "/health/ready",
                HealthyHttpCodes = "200",
                Interval = Duration.Seconds(30),
                Timeout = Duration.Seconds(5),
                HealthyThresholdCount = 2,
                UnhealthyThresholdCount = 3
            });

        data.DatabaseSecret.GrantRead(ExecutionRole);
        data.DatabaseSecret.GrantRead(MigrationRole);
        storage.AttachmentBucket.GrantReadWrite(TaskRole);
        _ = new CfnSecurityGroupIngress(
            this,
            "ApiPostgresIngress",
            new CfnSecurityGroupIngressProps
            {
                GroupId = network.DatabaseSecurityGroup.SecurityGroupId,
                IpProtocol = "tcp",
                FromPort = 5433,
                ToPort = 5433,
                SourceSecurityGroupId = ApiSecurityGroup.SecurityGroupId,
                Description = "API task access to PostgreSQL."
            });

        _ = new CfnSecurityGroupIngress(
            this,
            "MigrationPostgresIngress",
            new CfnSecurityGroupIngressProps
            {
                GroupId = network.DatabaseSecurityGroup.SecurityGroupId,
                IpProtocol = "tcp",
                FromPort = 5433,
                ToPort = 5433,
                SourceSecurityGroupId = MigrationSecurityGroup.SecurityGroupId,
                Description = "Migration task access to PostgreSQL."
            });

        TaskRole.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps
                {
                    Effect = Effect.ALLOW,
                    Actions = ["ssmmessages:CreateControlChannel", "ssmmessages:CreateDataChannel", "ssmmessages:OpenControlChannel", "ssmmessages:OpenDataChannel"],
                    Resources = ["*"]
                }));

        _ = new CfnOutput(
            this,
            "ApiRepositoryUri",
            new CfnOutputProps { Value = ApiRepository.RepositoryUri });
        _ = new CfnOutput(
            this,
            "ApiLoadBalancerDnsName",
            new CfnOutputProps { Value = ApiService.LoadBalancer.LoadBalancerDnsName });
        _ = new CfnOutput(
            this,
            "ApiClusterName",
            new CfnOutputProps { Value = Cluster.ClusterName });
        _ = new CfnOutput(
            this,
            "MigrationTaskDefinitionArn",
            new CfnOutputProps { Value = MigrationTaskDefinition.TaskDefinitionArn });
        _ = new CfnOutput(
            this,
            "MigrationSecurityGroupId",
            new CfnOutputProps { Value = MigrationSecurityGroup.SecurityGroupId });
        _ = new CfnOutput(
            this,
            "PrivateSubnetIds",
            new CfnOutputProps
            {
                Value = Fn.Join(",", network.Vpc.PrivateSubnets.Select(static subnet => subnet.SubnetId).ToArray())
            });

        Amazon.CDK.Tags.Of(this).Add("Application", "HouseKeeper");
        Amazon.CDK.Tags.Of(this).Add("Environment", configuration.EnvironmentName);
        Amazon.CDK.Tags.Of(this).Add("ManagedBy", "AWS-CDK");

        NagSuppressions.AddResourceSuppressions(
            TaskRole,
            new[]
            {
                new NagPackSuppression
                {
                    Id = "AwsSolutions-IAM5",
                    Reason = "S3 object permissions and ECS Exec channel permissions require resource wildcards generated by their AWS APIs."
                }
            },
            true);
        NagSuppressions.AddResourceSuppressions(
            ExecutionRole,
            new[]
            {
                new NagPackSuppression
                {
                    Id = "AwsSolutions-IAM5",
                    Reason = "ECR authorization and CloudWatch log delivery require AWS-managed wildcard resources; repository and log writes remain resource-scoped."
                }
            },
            true);
        NagSuppressions.AddStackSuppressions(
            this,
            new[]
            {
                new NagPackSuppression
                {
                    Id = "AwsSolutions-ECS2",
                    Reason = "These values are non-secret endpoint and protocol configuration; the database credential is injected from Secrets Manager."
                },
                new NagPackSuppression
                {
                    Id = "AwsSolutions-ELB2",
                    Reason = "ALB access logging is deferred for the shared development environment to avoid a second log bucket; request and application logs remain available."
                },
                new NagPackSuppression
                {
                    Id = "AwsSolutions-EC23",
                    Reason = "The public ALB is the documented internet entry point and forwards only to private ECS tasks."
                }
            });
    }

    public Repository ApiRepository { get; }

    public Cluster Cluster { get; }

    public ApplicationLoadBalancedFargateService ApiService { get; }

    public Role TaskRole { get; }

    public Role ExecutionRole { get; }

    public Role MigrationRole { get; }

    public FargateTaskDefinition MigrationTaskDefinition { get; }

    public SecurityGroup ApiSecurityGroup { get; }

    public SecurityGroup MigrationSecurityGroup { get; }

    public LogGroup ApiLogGroup { get; }

    public LogGroup MigrationLogGroup { get; }

    private LogGroup CreateLogGroup(
        string id,
        string purpose,
        PlatformConfiguration configuration) => new(
            this,
            id,
            new LogGroupProps
            {
                LogGroupName = $"/housekeeper/{configuration.EnvironmentName}/{purpose}",
                Retention = configuration.IsProduction
                    ? RetentionDays.ONE_YEAR
                    : RetentionDays.ONE_MONTH,
                RemovalPolicy = configuration.IsProduction
                    ? RemovalPolicy.RETAIN
                    : RemovalPolicy.DESTROY
            });
}
