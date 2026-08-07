using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.SSM;
using Cdklabs.CdkNag;

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
                Port = 5433,
                BackupRetention = Duration.Days(configuration.IsProduction ? 35 : 0),
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

        List<NagPackSuppression> databaseSuppressions =
        [
            new NagPackSuppression
            {
                Id = "AwsSolutions-RDS3",
                Reason = "Shared development deliberately uses a single-AZ burstable instance to keep the disposable environment within budget."
            },
            new NagPackSuppression
            {
                Id = "AwsSolutions-RDS10",
                Reason = "Shared development is disposable and teardown is documented; production enables deletion protection."
            }
        ];
        if (!configuration.IsProduction)
        {
            databaseSuppressions.Add(
                new NagPackSuppression
                {
                    Id = "AwsSolutions-RDS13",
                    Reason = "The shared-development account is on an AWS free-tier plan that rejects automated backup retention; production retains automated backups."
                });
        }

        NagSuppressions.AddResourceSuppressions(Database, databaseSuppressions.ToArray());
        NagSuppressions.AddResourceSuppressions(
            DatabaseSecret,
            new[]
            {
                new NagPackSuppression
                {
                    Id = "AwsSolutions-SMG4",
                    Reason = "The RDS-generated secret is scoped to the disposable development database; production secret rotation is a separate hardening step."
                }
            });
        NagSuppressions.AddStackSuppressions(
            this,
            new[]
            {
                new NagPackSuppression
                {
                    Id = "AwsSolutions-SMG4",
                    Reason = "The RDS-generated secret is scoped to the disposable development database; production secret rotation is a separate hardening step."
                }
            });

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
