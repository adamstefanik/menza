var builder = DistributedApplication.CreateBuilder(args);

var mysql = builder.AddMySql("mysql")
                   .WithDataVolume()
                   .WithLifetime(ContainerLifetime.Persistent)
                   .WithPhpMyAdmin();

var database = mysql.AddDatabase("database");

builder.AddProject<Projects.UTB_Library_DbManager>("dbmanager")
       .WithReference(database)
       .WithHttpCommand("reset-db", "Reset Database")
       .WaitFor(database);

builder.Build().Run();