using Amazon.CDK;
using Amazon.CDK.AWS.CloudWatch;
using Amazon.CDK.AWS.Logs;

using Constructs;

using HouseKeeper.Infrastructure.Configuration;

namespace HouseKeeper.Infrastructure.Stacks;

public sealed class ObservabilityStack : Stack
{
    public ObservabilityStack(
        Construct scope,
        string id,
        StackProps props,
        PlatformConfiguration configuration,
        ApplicationStack application,
        DataStack data)
        : base(scope, id, props)
    {
        ApiLogGroup = new LogGroup(
            this,
            "ApiLogGroup",
            new LogGroupProps
            {
                LogGroupName = $"/housekeeper/{configuration.EnvironmentName}/api",
                Retention = configuration.IsProduction
                    ? RetentionDays.ONE_YEAR
                    : RetentionDays.ONE_MONTH,
                RemovalPolicy = configuration.IsProduction
                    ? RemovalPolicy.RETAIN
                    : RemovalPolicy.DESTROY
            });

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

        MonthlyBudget = new Amazon.CDK.AWS.Budgets.CfnBudget(
            this,
            "MonthlyBudget",
            new Amazon.CDK.AWS.Budgets.CfnBudgetProps
            {
                Budget = new Amazon.CDK.AWS.Budgets.CfnBudget.BudgetDataProperty
                {
                    BudgetName = $"housekeeper-{configuration.EnvironmentName}-monthly",
                    BudgetType = "COST",
                    TimeUnit = "MONTHLY",
                    BudgetLimit = new Amazon.CDK.AWS.Budgets.CfnBudget.SpendProperty
                    {
                        Amount = configuration.IsProduction ? 250 : 75,
                        Unit = "USD"
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

    public Amazon.CDK.AWS.Budgets.CfnBudget MonthlyBudget { get; }
}
