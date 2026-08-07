using Amazon.CDK;
using Amazon.CDK.AWS.Budgets;

using Constructs;

using HouseKeeper.Infrastructure.Configuration;

namespace HouseKeeper.Infrastructure.Stacks;

public sealed class BudgetStack : Stack
{
    public BudgetStack(
        Construct scope,
        string id,
        StackProps props,
        PlatformConfiguration configuration)
        : base(scope, id, props)
    {
        MonthlyBudget = new CfnBudget(
            this,
            "MonthlyBudget",
            new CfnBudgetProps
            {
                Budget = new CfnBudget.BudgetDataProperty
                {
                    BudgetName = $"housekeeper-{configuration.EnvironmentName}-monthly",
                    BudgetType = "COST",
                    TimeUnit = "MONTHLY",
                    BudgetLimit = new CfnBudget.SpendProperty
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

    public CfnBudget MonthlyBudget { get; }
}
