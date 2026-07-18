var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddPostgres("postgres")
    .WithImage("postgres")
    .WithImageTag("18.4")
    .WithDataVolume("housekeeper-postgres-data");

var database = postgres.AddDatabase("housekeeper");

var storage = builder
    .AddAzureStorage("storage")
    .RunAsEmulator(emulator =>
    {
        emulator.WithDataVolume("housekeeper-azurite-data");
    });

var attachments = storage.AddBlobs("attachments");

builder
    .AddProject<Projects.HouseKeeper_Api>("api")
    .WithReference(database)
    .WithReference(attachments)
    .WaitFor(database)
    .WaitFor(attachments);

builder.Build().Run();
