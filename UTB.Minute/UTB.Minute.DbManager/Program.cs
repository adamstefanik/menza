using UTB.Minute.Db;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<CanteenContext>("database");

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapPost("/reset-db", async (CanteenContext context) =>
{
    await context.Database.EnsureDeletedAsync();
    await context.Database.EnsureCreatedAsync();

    // Seed meals
    var meal1 = new Meal { Description = "Chicken Schnitzel with Fries", Price = 120 };
    var meal2 = new Meal { Description = "Beef Goulash with Dumplings", Price = 135 };
    var meal3 = new Meal { Description = "Vegetable Risotto", Price = 105 };
    var meal4 = new Meal { Description = "Grilled Salmon with Potatoes", Price = 155, IsActive = false };

    context.Meals.AddRange(meal1, meal2, meal3, meal4);
    await context.SaveChangesAsync();

    // Seed menu items for today
    var today = DateOnly.FromDateTime(DateTime.Today);

    var menuItem1 = new MenuItem { Date = today, AvailablePortions = 10, MealId = meal1.Id };
    var menuItem2 = new MenuItem { Date = today, AvailablePortions = 8, MealId = meal2.Id };
    var menuItem3 = new MenuItem { Date = today, AvailablePortions = 0, MealId = meal3.Id };

    context.MenuItems.AddRange(menuItem1, menuItem2, menuItem3);
    await context.SaveChangesAsync();

    // Seed orders
    var order1 = new Order { MenuItemId = menuItem1.Id, Status = OrderStatus.Preparing };
    var order2 = new Order { MenuItemId = menuItem2.Id, Status = OrderStatus.Ready };

    context.Orders.AddRange(order1, order2);
    await context.SaveChangesAsync();

    return Results.Ok("Database has been reset and seeded successfully.");
});

app.UseHttpsRedirection();
app.Run();