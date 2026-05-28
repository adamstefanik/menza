using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Keycloak
var keycloak = builder.AddKeycloak("keycloak", 8080)
                      .WithDataVolume()
                      .WithRealmImport("../Keycloak"); // We will create this directory with a basic realm json if needed, or assume manual configuration. Actually, let's just create a basic realm later or let Keycloak start fresh. The requirement says "Keycloak spuštěn přes Aspire".

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
    .WithReference(keycloak)
    .WaitFor(database)
    .WaitFor(keycloak)
    .WaitFor(dbManager);

builder.AddProject<Projects.UTB_Minute_AdminClient>("adminclient")
    .WithReference(webApi)
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.UTB_Minute_CanteenClient>("canteenclient")
    .WithReference(webApi)
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithExternalHttpEndpoints();

builder.Build().Run();