using UTB.Library.Db;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ZMENA: Pre Mac a Docker používame MySql namiesto SqlServer
// Názov "database" musí presne sedieť s tým, čo máme v AppHoste
builder.AddMySqlDbContext<LibraryContext>("database");

var app = builder.Build();

// Náš endpoint na resetovanie a naplnenie (seeding) databázy
app.MapPost("/reset-db", async (LibraryContext context) =>
{
    await context.Database.EnsureDeletedAsync();
    await context.Database.EnsureCreatedAsync();

    Author a1 = new() { Name = "Karel Capek" };
    Author a2 = new() { Name = "Jaroslav Hasek" };
    Author a3 = new() { Name = "Bohumil Hrabal" };

    context.Authors.AddRange(a1, a2, a3);

    await context.SaveChangesAsync();

    return Results.Ok("Databaza bola uspesne resetovana a naplnena.");
});

app.UseHttpsRedirection();
app.Run();