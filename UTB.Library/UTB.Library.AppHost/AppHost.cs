var builder = DistributedApplication.CreateBuilder(args);

var mysql = builder.AddMySql("mysql")
                   .WithDataVolume()
                   .WithLifetime(ContainerLifetime.Persistent)
                   .WithPhpMyAdmin();

var database = mysql.AddDatabase("database");

builder.AddProject<Projects.UTB_Library_DbManager>("dbmanager")
       .WithReference(database);

builder.Build().Run();