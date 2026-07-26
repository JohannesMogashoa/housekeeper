using Amazon.CDK;

using HouseKeeper.Infrastructure.Configuration;
using HouseKeeper.Infrastructure.Stacks;

PlatformConfiguration configuration = PlatformConfiguration.Load();
App app = new();

Amazon.CDK.Environment? environment = configuration.Account is null
    ? new Amazon.CDK.Environment { Region = configuration.Region }
    : new Amazon.CDK.Environment
    {
        Account = configuration.Account,
        Region = configuration.Region
    };

NetworkStack network = new(app, "HouseKeeperNetwork", new StackProps { Env = environment }, configuration);
DataStack data = new(app, "HouseKeeperData", new StackProps { Env = environment }, configuration, network);
IdentityStack identity = new(app, "HouseKeeperIdentity", new StackProps { Env = environment }, configuration);
StorageStack storage = new(app, "HouseKeeperStorage", new StackProps { Env = environment }, configuration);
ApplicationStack application = new(
    app,
    "HouseKeeperApplication",
    new StackProps { Env = environment },
    configuration,
    network,
    data,
    storage,
    identity);
DeliveryStack delivery = new(app, "HouseKeeperDelivery", new StackProps { Env = environment }, configuration);
ObservabilityStack observability = new(
    app,
    "HouseKeeperObservability",
    new StackProps { Env = environment },
    configuration,
    application,
    data);

app.Synth();
