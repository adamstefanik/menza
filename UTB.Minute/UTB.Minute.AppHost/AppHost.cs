var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
                      .WithDataVolume()
                      .WithLifetime(ContainerLifetime.Persistent)
                      .WithPgAdmin();

var database = postgres.AddDatabase("database");

var dbManager = builder.AddProject<Projects.UTB_Minute_DbManager>("dbmanager")
    .WithReference(database)
    .WithHttpCommand("reset-db", "Reset Database")
    .WaitFor(database);

var webApi = builder.AddProject<Projects.UTB_Minute_WebApi>("webapi")
    .WithReference(database)
    .WaitFor(database)
    .WaitFor(dbManager);

builder.Build().Run();