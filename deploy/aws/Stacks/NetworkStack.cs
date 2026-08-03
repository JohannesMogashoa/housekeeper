using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Logs;
using Cdklabs.CdkNag;

using Constructs;

using HouseKeeper.Infrastructure.Configuration;

namespace HouseKeeper.Infrastructure.Stacks;

public sealed class NetworkStack : Stack
{
    public NetworkStack(
        Construct scope,
        string id,
        StackProps props,
        PlatformConfiguration configuration)
        : base(scope, id, props)
    {
        Vpc = new Vpc(
            this,
            "Vpc",
            new VpcProps
            {
                VpcName = $"housekeeper-{configuration.EnvironmentName}",
                AvailabilityZones = ["af-south-1a", "af-south-1b"],
                NatGateways = configuration.IsProduction ? 2 : 1,
                SubnetConfiguration =
                [
                    new SubnetConfiguration
                    {
                        Name = "Public",
                        SubnetType = SubnetType.PUBLIC,
                        CidrMask = 24
                    },
                    new SubnetConfiguration
                    {
                        Name = "Application",
                        SubnetType = SubnetType.PRIVATE_WITH_EGRESS,
                        CidrMask = 24
                    },
                    new SubnetConfiguration
                    {
                        Name = "Database",
                        SubnetType = SubnetType.PRIVATE_ISOLATED,
                        CidrMask = 24
                    }
                ],
                EnableDnsHostnames = true,
                EnableDnsSupport = true
            });

        FlowLogRole = new Role(
            this,
            "FlowLogRole",
            new RoleProps
            {
                AssumedBy = new ServicePrincipal("vpc-flow-logs.amazonaws.com"),
                Description = "VPC flow log delivery role for the isolated HouseKeeper environment."
            });
        FlowLogGroup = new LogGroup(
            this,
            "FlowLogGroup",
            new LogGroupProps
            {
                LogGroupName = $"/housekeeper/{configuration.EnvironmentName}/vpc-flow-logs",
                Retention = configuration.IsProduction
                    ? RetentionDays.ONE_YEAR
                    : RetentionDays.ONE_MONTH,
                RemovalPolicy = configuration.IsProduction
                    ? RemovalPolicy.RETAIN
                    : RemovalPolicy.DESTROY
            });
        FlowLogRole.AddToPolicy(
            new PolicyStatement(
                new PolicyStatementProps
                {
                    Effect = Effect.ALLOW,
                    Actions = ["logs:CreateLogStream", "logs:DescribeLogStreams", "logs:PutLogEvents"],
                    Resources = [FlowLogGroup.LogGroupArn, $"{FlowLogGroup.LogGroupArn}:*"]
                }));
        _ = Vpc.AddFlowLog(
            "FlowLog",
            new FlowLogOptions
            {
                Destination = FlowLogDestination.ToCloudWatchLogs(FlowLogGroup, FlowLogRole),
                TrafficType = FlowLogTrafficType.ALL
            });
        NagSuppressions.AddResourceSuppressions(
            FlowLogRole,
            new[]
            {
                new NagPackSuppression
                {
                    Id = "AwsSolutions-IAM5",
                    Reason = "VPC flow-log delivery must write to all streams within this dedicated log group."
                }
            },
            true);

        DatabaseSecurityGroup = new SecurityGroup(
            this,
            "DatabaseSecurityGroup",
            new SecurityGroupProps
            {
                Vpc = Vpc,
                Description = "HouseKeeper RDS accepts PostgreSQL only from API tasks and migrations.",
                AllowAllOutbound = false
            });

        Amazon.CDK.Tags.Of(this).Add("Application", "HouseKeeper");
        Amazon.CDK.Tags.Of(this).Add("Environment", configuration.EnvironmentName);
        Amazon.CDK.Tags.Of(this).Add("ManagedBy", "AWS-CDK");
        Amazon.CDK.Tags.Of(this).Add("DataClassification", "Household-private");
    }

    public Vpc Vpc { get; }

    public SecurityGroup DatabaseSecurityGroup { get; }

    public Role FlowLogRole { get; }

    public LogGroup FlowLogGroup { get; }
}
