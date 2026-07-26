using Amazon.CDK;
using Amazon.CDK.AWS.EC2;

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
                MaxAzs = 2,
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
}
