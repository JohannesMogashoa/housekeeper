using Amazon.CDK;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.IAM;

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
        ExecutionRole.AddManagedPolicy(
            ManagedPolicy.FromAwsManagedPolicyName("service-role/AmazonECSTaskExecutionRolePolicy"));

        Dictionary<string, string> taskEnvironment = new()
        {
            ["ASPNETCORE_HTTP_PORTS"] = "8080",
            ["HOUSEKEEPER_AWS_REGION"] = configuration.Region,
            ["Authentication__Mode"] = "Cognito",
            ["Authentication__Cognito__Authority"] = identity.Issuer,
            ["Authentication__Cognito__ClientId"] = identity.WebClient.UserPoolClientId
        };
        string[] pwaOrigins = configuration.CallbackUrls
            .Select(static url => new Uri(url).GetLeftPart(UriPartial.Authority))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        for (int index = 0; index < pwaOrigins.Length; index++)
        {
            taskEnvironment[$"Cors__AllowedOrigins__{index}"] = pwaOrigins[index];
        }

        ApiService = new ApplicationLoadBalancedFargateService(
            this,
            "ApiService",
            new ApplicationLoadBalancedFargateServiceProps
            {
                Cluster = Cluster,
                Cpu = 512,
                MemoryLimitMiB = 1024,
                DesiredCount = 1,
                PublicLoadBalancer = true,
                AssignPublicIp = false,
                SecurityGroups = [ApiSecurityGroup],
                TaskSubnets = new SubnetSelection
                {
                    SubnetType = SubnetType.PRIVATE_WITH_EGRESS
                },
                TaskImageOptions = new ApplicationLoadBalancedTaskImageOptions
                {
                    Image = ContainerImage.FromEcrRepository(ApiRepository, "bootstrap"),
                    ContainerName = "housekeeper-api",
                    ContainerPort = 8080,
                    EnableLogging = true,
                    ExecutionRole = ExecutionRole,
                    TaskRole = TaskRole,
                    Environment = taskEnvironment,
                    Secrets = new Dictionary<string, EcsSecret>
                    {
                        ["ConnectionStrings__HouseKeeper"] = EcsSecret.FromSecretsManager(data.DatabaseSecret)
                    }
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
        storage.AttachmentBucket.GrantReadWrite(TaskRole);
        _ = new CfnSecurityGroupIngress(
            this,
            "ApiPostgresIngress",
            new CfnSecurityGroupIngressProps
            {
                GroupId = network.DatabaseSecurityGroup.SecurityGroupId,
                IpProtocol = "tcp",
                FromPort = 5432,
                ToPort = 5432,
                SourceSecurityGroupId = ApiSecurityGroup.SecurityGroupId,
                Description = "API task access to PostgreSQL."
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

        Amazon.CDK.Tags.Of(this).Add("Application", "HouseKeeper");
        Amazon.CDK.Tags.Of(this).Add("Environment", configuration.EnvironmentName);
        Amazon.CDK.Tags.Of(this).Add("ManagedBy", "AWS-CDK");
    }

    public Repository ApiRepository { get; }

    public Cluster Cluster { get; }

    public ApplicationLoadBalancedFargateService ApiService { get; }

    public Role TaskRole { get; }

    public Role ExecutionRole { get; }

    public SecurityGroup ApiSecurityGroup { get; }
}
