using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using UTB.Minute.Contracts; // for users
using UTB.Minute.Db; // for backend

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<CanteenContext>("database");

var app = builder.Build();

app.MapDefaultEndpoints();

// MEALS 

app.MapGet("/api/meals", GetMeals);
app.MapGet("/api/meals/{id:int}", GetMeal);
app.MapPost("/api/meals", CreateMeal);
app.MapPut("/api/meals/{id:int}", UpdateMeal);

// MENU ITEMS

app.MapGet("/api/menu", GetMenuItems);
app.MapGet("/api/menu/today", GetTodayMenu);
app.MapPost("/api/menu", CreateMenuItem);
app.MapPut("/api/menu/{id:int}", UpdateMenuItem);
app.MapDelete("/api/menu/{id:int}", DeleteMenuItem);

// ORDERS

app.MapGet("/api/orders", GetOrders);
app.MapPost("/api/orders", CreateOrder);
app.MapPut("/api/orders/{id:int}/status", UpdateOrderStatus);

app.UseHttpsRedirection();
app.Run();

// MEAL HANDLERS

static async Task<Ok<List<MealDto>>> GetMeals(CanteenContext db)
{
    var meals = await db.Meals
        .Select(m => new MealDto(m.Id, m.Description, m.Price, m.IsActive))
        .ToListAsync();

    return TypedResults.Ok(meals);
}

static async Task<Results<Ok<MealDto>, NotFound>> GetMeal(int id, CanteenContext db)
{
    if (await db.Meals.FindAsync(id) is Meal meal)
    {
        return TypedResults.Ok(new MealDto(meal.Id, meal.Description, meal.Price, meal.IsActive));
    }

    return TypedResults.NotFound();
}

static async Task<Created<MealDto>> CreateMeal(CreateMealDto dto, CanteenContext db)
{
    var meal = new Meal { Description = dto.Description, Price = dto.Price };

    db.Meals.Add(meal);
    await db.SaveChangesAsync();

    return TypedResults.Created($"/api/meals/{meal.Id}", new MealDto(meal.Id, meal.Description, meal.Price, meal.IsActive));
}

static async Task<Results<Ok<MealDto>, NotFound>> UpdateMeal(int id, UpdateMealDto dto, CanteenContext db)
{
    if (await db.Meals.FindAsync(id) is Meal meal)
    {
        meal.Description = dto.Description;
        meal.Price = dto.Price;
        meal.IsActive = dto.IsActive;

        await db.SaveChangesAsync();

        return TypedResults.Ok(new MealDto(meal.Id, meal.Description, meal.Price, meal.IsActive));
    }

    return TypedResults.NotFound();
}

// MENU ITEM HANDLERS

static async Task<Ok<List<MenuItemDto>>> GetMenuItems(CanteenContext db)
{
    var items = await db.MenuItems
        .Include(mi => mi.Meal)
        .Select(mi => new MenuItemDto(mi.Id, mi.Date, mi.AvailablePortions, mi.MealId, mi.Meal.Description))
        .ToListAsync();

    return TypedResults.Ok(items);
}

static async Task<Ok<List<MenuItemDto>>> GetTodayMenu(CanteenContext db)
{
    var today = DateOnly.FromDateTime(DateTime.Today);

    var items = await db.MenuItems
        .Include(mi => mi.Meal)
        .Where(mi => mi.Date == today)
        .Select(mi => new MenuItemDto(mi.Id, mi.Date, mi.AvailablePortions, mi.MealId, mi.Meal.Description))
        .ToListAsync();

    return TypedResults.Ok(items);
}

static async Task<Created<MenuItemDto>> CreateMenuItem(CreateMenuItemDto dto, CanteenContext db)
{
    var menuItem = new MenuItem { Date = dto.Date, AvailablePortions = dto.AvailablePortions, MealId = dto.MealId };

    db.MenuItems.Add(menuItem);
    await db.SaveChangesAsync();

    var created = await db.MenuItems.Include(mi => mi.Meal).FirstAsync(mi => mi.Id == menuItem.Id);

    return TypedResults.Created($"/api/menu/{menuItem.Id}",
        new MenuItemDto(created.Id, created.Date, created.AvailablePortions, created.MealId, created.Meal.Description));
}

static async Task<Results<Ok<MenuItemDto>, NotFound>> UpdateMenuItem(int id, UpdateMenuItemDto dto, CanteenContext db)
{
    var menuItem = await db.MenuItems.Include(mi => mi.Meal).FirstOrDefaultAsync(mi => mi.Id == id);

    if (menuItem is not null)
    {
        menuItem.Date = dto.Date;
        menuItem.AvailablePortions = dto.AvailablePortions;

        await db.SaveChangesAsync();

        return TypedResults.Ok(new MenuItemDto(menuItem.Id, menuItem.Date, menuItem.AvailablePortions, menuItem.MealId, menuItem.Meal.Description));
    }

    return TypedResults.NotFound();
}

static async Task<Results<NoContent, NotFound>> DeleteMenuItem(int id, CanteenContext db)
{
    if (await db.MenuItems.FindAsync(id) is MenuItem menuItem)
    {
        db.MenuItems.Remove(menuItem);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    return TypedResults.NotFound();
}

// ORDER HANDLERS

static async Task<Ok<List<OrderDto>>> GetOrders(CanteenContext db)
{
    var orders = await db.Orders
        .Include(o => o.MenuItem)
        .ThenInclude(mi => mi.Meal)
        .Select(o => new OrderDto(o.Id, o.Status.ToString(), o.CreatedAt, o.MenuItemId, o.MenuItem.Meal.Description))
        .ToListAsync();

    return TypedResults.Ok(orders);
}

static async Task<Results<Created<OrderDto>, NotFound, BadRequest<string>>> CreateOrder(CreateOrderDto dto, CanteenContext db)
{
    var menuItem = await db.MenuItems.Include(mi => mi.Meal).FirstOrDefaultAsync(mi => mi.Id == dto.MenuItemId);

    if (menuItem is null)
    {
        return TypedResults.NotFound();
    }

    if (menuItem.AvailablePortions <= 0)
    {
        return TypedResults.BadRequest("This meal is sold out.");
    }

    menuItem.AvailablePortions--;

    var order = new Order { MenuItemId = dto.MenuItemId, Status = OrderStatus.Preparing };
    db.Orders.Add(order);
    await db.SaveChangesAsync();

    return TypedResults.Created($"/api/orders/{order.Id}",
        new OrderDto(order.Id, order.Status.ToString(), order.CreatedAt, order.MenuItemId, menuItem.Meal.Description));
}

static async Task<Results<Ok<OrderDto>, NotFound, BadRequest<string>>> UpdateOrderStatus(int id, UpdateOrderStatusDto dto, CanteenContext db)
{
    var order = await db.Orders
        .Include(o => o.MenuItem)
        .ThenInclude(mi => mi.Meal)
        .FirstOrDefaultAsync(o => o.Id == id);

    if (order is null)
    {
        return TypedResults.NotFound();
    }

    if (!Enum.TryParse<OrderStatus>(dto.Status, true, out var newStatus))
    {
        return TypedResults.BadRequest("Invalid status value.");
    }

    var valid = (order.Status, newStatus) switch
    {
        (OrderStatus.Preparing, OrderStatus.Ready) => true,
        (OrderStatus.Preparing, OrderStatus.Cancelled) => true,
        (OrderStatus.Ready, OrderStatus.Completed) => true,
        (OrderStatus.Cancelled, OrderStatus.Completed) => true,
        _ => false
    };

    if (!valid)
    {
        return TypedResults.BadRequest($"Cannot transition from {order.Status} to {newStatus}.");
    }

    order.Status = newStatus;
    await db.SaveChangesAsync();

    return TypedResults.Ok(new OrderDto(order.Id, order.Status.ToString(), order.CreatedAt, order.MenuItemId, order.MenuItem.Meal.Description));
}

public partial class Program { }
