using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.SSM;

using Constructs;

using HouseKeeper.Infrastructure.Configuration;

namespace HouseKeeper.Infrastructure.Stacks;

public sealed class DataStack : Stack
{
    public DataStack(
        Construct scope,
        string id,
        StackProps props,
        PlatformConfiguration configuration,
        NetworkStack network)
        : base(scope, id, props)
    {
        Database = new DatabaseInstance(
            this,
            "Postgres",
            new DatabaseInstanceProps
            {
                Engine = DatabaseInstanceEngine.Postgres(
                    new PostgresInstanceEngineProps
                    {
                        Version = PostgresEngineVersion.VER_17
                    }),
                InstanceIdentifier = $"housekeeper-{configuration.EnvironmentName}-postgres",
                DatabaseName = "housekeeper",
                Credentials = Credentials.FromGeneratedSecret("housekeeper_admin"),
                InstanceType = Amazon.CDK.AWS.EC2.InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.MICRO),
                Vpc = network.Vpc,
                VpcSubnets = new SubnetSelection
                {
                    SubnetType = SubnetType.PRIVATE_ISOLATED
                },
                SecurityGroups = [network.DatabaseSecurityGroup],
                AllocatedStorage = 20,
                MaxAllocatedStorage = 100,
                StorageType = StorageType.GP3,
                StorageEncrypted = true,
                BackupRetention = Duration.Days(configuration.IsProduction ? 35 : 7),
                CopyTagsToSnapshot = true,
                DeletionProtection = configuration.IsProduction,
                DeleteAutomatedBackups = !configuration.IsProduction,
                MultiAz = configuration.IsProduction,
                PubliclyAccessible = false,
                AutoMinorVersionUpgrade = true,
                CloudwatchLogsExports = ["postgresql"],
                RemovalPolicy = configuration.IsProduction
                    ? RemovalPolicy.RETAIN
                    : RemovalPolicy.SNAPSHOT
            });

        DatabaseSecret = Database.Secret
            ?? throw new InvalidOperationException("RDS did not produce a credentials secret.");

        _ = new StringParameter(
            this,
            "DatabaseEndpointParameter",
            new StringParameterProps
            {
                ParameterName = $"/housekeeper/{configuration.EnvironmentName}/database/endpoint",
                StringValue = Database.DbInstanceEndpointAddress,
                Description = "RDS PostgreSQL endpoint for migration and runtime configuration discovery.",
                Tier = ParameterTier.STANDARD
            });

        Amazon.CDK.Tags.Of(this).Add("Application", "HouseKeeper");
        Amazon.CDK.Tags.Of(this).Add("Environment", configuration.EnvironmentName);
        Amazon.CDK.Tags.Of(this).Add("ManagedBy", "AWS-CDK");
        Amazon.CDK.Tags.Of(this).Add("DataClassification", "Household-private");
    }

    public DatabaseInstance Database { get; }

    public Amazon.CDK.AWS.SecretsManager.ISecret DatabaseSecret { get; }
}
