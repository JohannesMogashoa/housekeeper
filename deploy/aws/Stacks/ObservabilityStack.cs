using Amazon.CDK;
using Amazon.CDK.AWS.CloudWatch;
using Amazon.CDK.AWS.Logs;

using Constructs;

using HouseKeeper.Infrastructure.Configuration;

namespace HouseKeeper.Infrastructure.Stacks;

public sealed class ObservabilityStack : Stack
{
    private static readonly string[] AllSupportedResourceTypes = ["AWS::AllSupported"];

    private static readonly string[] HouseKeeperTagValues = ["HouseKeeper"];

    public ObservabilityStack(
        Construct scope,
        string id,
        StackProps props,
        PlatformConfiguration configuration,
        ApplicationStack application,
        DataStack data)
        : base(scope, id, props)
    {
        ApiLogGroup = application.ApiLogGroup;

        ApiCpuAlarm = new Alarm(
            this,
            "ApiCpuAlarm",
            new AlarmProps
            {
                AlarmName = $"housekeeper-{configuration.EnvironmentName}-api-cpu",
                Metric = application.ApiService.Service.MetricCpuUtilization(
                    new MetricOptions
                    {
                        Period = Duration.Minutes(5),
                        Statistic = "Average"
                    }),
                Threshold = 80,
                EvaluationPeriods = 3,
                DatapointsToAlarm = 3,
                TreatMissingData = TreatMissingData.NOT_BREACHING
            });

        DatabaseCpuAlarm = new Alarm(
            this,
            "DatabaseCpuAlarm",
            new AlarmProps
            {
                AlarmName = $"housekeeper-{configuration.EnvironmentName}-database-cpu",
                Metric = data.Database.MetricCPUUtilization(
                    new MetricOptions
                    {
                        Period = Duration.Minutes(5),
                        Statistic = "Average"
                    }),
                Threshold = 80,
                EvaluationPeriods = 3,
                DatapointsToAlarm = 3,
                TreatMissingData = TreatMissingData.NOT_BREACHING
            });

        XRayGroup = new CfnResource(
            this,
            "XRayGroup",
            new CfnResourceProps
            {
                Type = "AWS::XRay::Group",
                Properties = new Dictionary<string, object>
                {
                    ["GroupName"] = $"housekeeper-{configuration.EnvironmentName}",
                    ["FilterExpression"] = "service(\"HouseKeeper.Api\")"
                }
            });

        ResourceGroup = new CfnResource(
            this,
            "ResourceGroup",
            new CfnResourceProps
            {
                Type = "AWS::ResourceGroups::Group",
                Properties = new Dictionary<string, object>
                {
                    ["Name"] = $"housekeeper-{configuration.EnvironmentName}",
                    ["Description"] = "HouseKeeper resources for one isolated environment.",
                    ["ResourceQuery"] = new Dictionary<string, object>
                    {
                        ["Type"] = "TAG_FILTERS_1_0",
                        ["Query"] = new Dictionary<string, object>
                        {
                            ["ResourceTypeFilters"] = AllSupportedResourceTypes,
                            ["TagFilters"] = new object[]
                            {
                                new Dictionary<string, object>
                                {
                                    ["Key"] = "Application",
                                    ["Values"] = HouseKeeperTagValues
                                },
                                new Dictionary<string, object>
                                {
                                    ["Key"] = "Environment",
                                    ["Values"] = new[] { configuration.EnvironmentName }
                                }
                            }
                        }
                    }
                }
            });
        Amazon.CDK.Tags.Of(this).Add("Application", "HouseKeeper");
        Amazon.CDK.Tags.Of(this).Add("Environment", configuration.EnvironmentName);
        Amazon.CDK.Tags.Of(this).Add("ManagedBy", "AWS-CDK");
    }

    public LogGroup ApiLogGroup { get; }

    public Alarm ApiCpuAlarm { get; }

    public Alarm DatabaseCpuAlarm { get; }

    public CfnResource XRayGroup { get; }

    public CfnResource ResourceGroup { get; }
}
