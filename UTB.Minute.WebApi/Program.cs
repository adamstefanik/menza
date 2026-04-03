using Microsoft.EntityFrameworkCore;
using UTB.Minute.Contracts;
using UTB.Minute.Db;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<CanteenContext>("database");

var app = builder.Build();

app.MapDefaultEndpoints();

// === MEALS ===

// TODO: maybe add filtering by isActive later
app.MapGet("/api/meals", async (CanteenContext db) =>
{
    var meals = await db.Meals
        .Select(m => new MealDto(m.Id, m.Description, m.Price, m.IsActive))
        .ToListAsync();

    return TypedResults.Ok(meals);
});

app.MapGet("/api/meals/{id}", async (int id, CanteenContext db) =>
{
    var meal = await db.Meals.FindAsync(id);

    return meal is null
        ? Results.NotFound()
        : Results.Ok(new MealDto(meal.Id, meal.Description, meal.Price, meal.IsActive));
});

app.MapPost("/api/meals", async (CreateMealDto dto, CanteenContext db) =>
{
    var meal = new Meal { Description = dto.Description, Price = dto.Price };

    db.Meals.Add(meal);
    await db.SaveChangesAsync();

    return TypedResults.Created($"/api/meals/{meal.Id}", new MealDto(meal.Id, meal.Description, meal.Price, meal.IsActive));
});

app.MapPut("/api/meals/{id}", async (int id, UpdateMealDto dto, CanteenContext db) =>
{
    var meal = await db.Meals.FindAsync(id);
    if (meal is null) return Results.NotFound();

    meal.Description = dto.Description;
    meal.Price = dto.Price;
    meal.IsActive = dto.IsActive;

    await db.SaveChangesAsync();

    return Results.Ok(new MealDto(meal.Id, meal.Description, meal.Price, meal.IsActive));
});

// === MENU ITEMS ===

app.MapGet("/api/menu", async (CanteenContext db) =>
{
    var items = await db.MenuItems
        .Include(mi => mi.Meal)
        .Select(mi => new MenuItemDto(mi.Id, mi.Date, mi.AvailablePortions, mi.MealId, mi.Meal.Description))
        .ToListAsync();

    return TypedResults.Ok(items);
});

app.MapGet("/api/menu/today", async (CanteenContext db) =>
{
    var today = DateOnly.FromDateTime(DateTime.Today);

    var items = await db.MenuItems
        .Include(mi => mi.Meal)
        .Where(mi => mi.Date == today)
        .Select(mi => new MenuItemDto(mi.Id, mi.Date, mi.AvailablePortions, mi.MealId, mi.Meal.Description))
        .ToListAsync();

    return TypedResults.Ok(items);
});

app.MapPost("/api/menu", async (CreateMenuItemDto dto, CanteenContext db) =>
{
    var menuItem = new MenuItem { Date = dto.Date, AvailablePortions = dto.AvailablePortions, MealId = dto.MealId };

    db.MenuItems.Add(menuItem);
    await db.SaveChangesAsync();

    var created = await db.MenuItems.Include(mi => mi.Meal).FirstAsync(mi => mi.Id == menuItem.Id);

    return TypedResults.Created($"/api/menu/{menuItem.Id}",
        new MenuItemDto(created.Id, created.Date, created.AvailablePortions, created.MealId, created.Meal.Description));
});

app.MapPut("/api/menu/{id}", async (int id, UpdateMenuItemDto dto, CanteenContext db) =>
{
    var menuItem = await db.MenuItems.Include(mi => mi.Meal).FirstOrDefaultAsync(mi => mi.Id == id);
    if (menuItem is null) return Results.NotFound();

    menuItem.Date = dto.Date;
    menuItem.AvailablePortions = dto.AvailablePortions;

    await db.SaveChangesAsync();

    return Results.Ok(new MenuItemDto(menuItem.Id, menuItem.Date, menuItem.AvailablePortions, menuItem.MealId, menuItem.Meal.Description));
});

app.MapDelete("/api/menu/{id}", async (int id, CanteenContext db) =>
{
    var menuItem = await db.MenuItems.FindAsync(id);
    if (menuItem is null) return Results.NotFound();

    db.MenuItems.Remove(menuItem);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

// === ORDERS ===

// TODO: filter only non-completed orders for the cook view
app.MapGet("/api/orders", async (CanteenContext db) =>
{
    var orders = await db.Orders
        .Include(o => o.MenuItem)
        .ThenInclude(mi => mi.Meal)
        .Select(o => new OrderDto(o.Id, o.Status.ToString(), o.CreatedAt, o.MenuItemId, o.MenuItem.Meal.Description))
        .ToListAsync();

    return TypedResults.Ok(orders);
});

app.MapPost("/api/orders", async (CreateOrderDto dto, CanteenContext db) =>
{
    var menuItem = await db.MenuItems.Include(mi => mi.Meal).FirstOrDefaultAsync(mi => mi.Id == dto.MenuItemId);
    if (menuItem == null) return Results.NotFound();
    if (menuItem.AvailablePortions <= 0) return Results.BadRequest("This meal is sold out.");

    menuItem.AvailablePortions--;

    var order = new Order { MenuItemId = dto.MenuItemId, Status = OrderStatus.Preparing };
    db.Orders.Add(order);
    await db.SaveChangesAsync();

    return Results.Created($"/api/orders/{order.Id}",
        new OrderDto(order.Id, order.Status.ToString(), order.CreatedAt, order.MenuItemId, menuItem.Meal.Description));
});

app.MapPut("/api/orders/{id}/status", async (int id, UpdateOrderStatusDto dto, CanteenContext db) =>
{
    var order = await db.Orders
        .Include(o => o.MenuItem)
        .ThenInclude(mi => mi.Meal)
        .FirstOrDefaultAsync(o => o.Id == id);

    if (order is null) return Results.NotFound();

    if (!Enum.TryParse<OrderStatus>(dto.Status, true, out var newStatus))
        return Results.BadRequest("Invalid status value.");

    var valid = (order.Status, newStatus) switch
    {
        (OrderStatus.Preparing, OrderStatus.Ready) => true,
        (OrderStatus.Preparing, OrderStatus.Cancelled) => true,
        (OrderStatus.Ready, OrderStatus.Completed) => true,
        (OrderStatus.Cancelled, OrderStatus.Completed) => true,
        _ => false
    };

    if (!valid)
        return Results.BadRequest($"Cannot transition from {order.Status} to {newStatus}.");

    order.Status = newStatus;
    await db.SaveChangesAsync();

    return Results.Ok(new OrderDto(order.Id, order.Status.ToString(), order.CreatedAt, order.MenuItemId, order.MenuItem.Meal.Description));
});

app.UseHttpsRedirection();
app.Run();

public partial class Program { }
