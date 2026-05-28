using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<PostgresServerResource> postgres;

if (builder.Environment.IsEnvironment("Testing"))
{
    postgres = builder.AddPostgres("postgres-testing")
                      .WithContainerName("postgres-testing-UTB.Minute");
}
else
{
    postgres = builder.AddPostgres("postgres")
                      .WithDataVolume()
                      .WithLifetime(ContainerLifetime.Persistent)
                      .WithPgAdmin();
}

var database = postgres.AddDatabase("database");

var dbManager = builder.AddProject<Projects.UTB_Minute_DbManager>("dbmanager")
    .WithReference(database)
    .WithHttpCommand("reset-db", "Reset Database")
    .WaitFor(database);

var webApi = builder.AddProject<Projects.UTB_Minute_WebApi>("webapi")
    .WithReference(database)
    .WaitFor(database)
    .WaitFor(dbManager);

builder.AddProject<Projects.UTB_Minute_AdminClient>("adminclient")
    .WithReference(webApi) 
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.UTB_Minute_CanteenClient>("canteenclient")
    .WithReference(webApi)
    .WithExternalHttpEndpoints();

builder.Build().Run();