using Amazon.CDK;
using Cdklabs.CdkNag;

using HouseKeeper.Infrastructure.Configuration;
using HouseKeeper.Infrastructure.Stacks;

PlatformConfiguration configuration = PlatformConfiguration.Load();
App app = new();
Aspects.Of(app).Add(new AwsSolutionsChecks());

Amazon.CDK.Environment? environment = configuration.Account is null
    ? new Amazon.CDK.Environment { Region = configuration.Region }
    : new Amazon.CDK.Environment
    {
        Account = configuration.Account,
        Region = configuration.Region
    };

GitHubOidcStack githubOidc = new(
    app,
    "HouseKeeperGitHubOidc",
    new StackProps
    {
        Env = environment,
        StackName = "HouseKeeper-GitHubOidc"
    });

NetworkStack network = new(app, "HouseKeeperNetwork", EnvironmentStackProps("Network"), configuration);
DataStack data = new(app, "HouseKeeperData", EnvironmentStackProps("Data"), configuration, network);
IdentityStack identity = new(app, "HouseKeeperIdentity", EnvironmentStackProps("Identity"), configuration);
StorageStack storage = new(app, "HouseKeeperStorage", EnvironmentStackProps("Storage"), configuration);
ApplicationStack application = new(
    app,
    "HouseKeeperApplication",
    EnvironmentStackProps("Application"),
    configuration,
    network,
    data,
    storage,
    identity);
DeliveryStack delivery = new(
    app,
    "HouseKeeperDelivery",
    EnvironmentStackProps("Delivery"),
    configuration,
    application,
    storage,
    githubOidc.Provider);
ObservabilityStack observability = new(
    app,
    "HouseKeeperObservability",
    EnvironmentStackProps("Observability"),
    configuration,
    application,
    data);
BudgetStack budget = new(
    app,
    "HouseKeeperBudget",
    BudgetStackProps(),
    configuration);

app.Synth();

StackProps EnvironmentStackProps(string suffix) => new()
{
    Env = environment,
    StackName = $"HouseKeeper-{configuration.EnvironmentName}-{suffix}"
};

StackProps BudgetStackProps() => new()
{
    Env = configuration.Account is null
        ? new Amazon.CDK.Environment { Region = "us-east-1" }
        : new Amazon.CDK.Environment
        {
            Account = configuration.Account,
            Region = "us-east-1"
        },
    StackName = $"HouseKeeper-{configuration.EnvironmentName}-Budget"
};
