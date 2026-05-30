using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UTB.Minute.Contracts; // for users
using UTB.Minute.Db; // for backend
using UTB.Minute.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<CanteenContext>("database");

builder.Services.AddSingleton<UTB.Minute.WebApi.Services.SseNotifier>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var keycloakUrl = builder.Configuration.GetConnectionString("keycloak");
        
        if (string.IsNullOrEmpty(keycloakUrl))
        {
            keycloakUrl = "http://localhost:8080"; 
        }
        else if (!keycloakUrl.Contains("://"))
        {
            keycloakUrl = "http://" + keycloakUrl; 
        }

        options.Authority = $"{keycloakUrl}/realms/menza";
        options.Audience = "account";
        options.RequireHttpsMetadata = false;

        // Use a custom handler to bypass SSL and force HTTP/1.1
        options.BackchannelHttpHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        
        options.Backchannel = new HttpClient(options.BackchannelHttpHandler)
        {
            DefaultRequestVersion = System.Net.HttpVersion.Version11,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact,
            Timeout = TimeSpan.FromSeconds(30)
        };

        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateAudience = false, 
            ValidateIssuer = false,   
            ValidateLifetime = false, 
            ValidateSignatureLast = false,
            SignatureValidator = delegate (string token, Microsoft.IdentityModel.Tokens.TokenValidationParameters parameters)
            {
                var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(token);
                return jwt;
            },
            RoleClaimType = "roles"
        };

        // Map "roles" claim and "realm_access" roles to the standard .NET Role claim
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError("[AUTH] Authentication failed: {Message}. Exception: {Exception}", context.Exception.Message, context.Exception.ToString());
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError("[AUTH] CHALLENGE: {Error} - {Description}", context.Error, context.ErrorDescription);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var identity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
                if (identity != null)
                {
                    // 1. Map flat "roles" claim (from our mapper)
                    var roles = identity.FindAll("roles").ToList();
                    foreach (var role in roles)
                    {
                        if (!identity.HasClaim(identity.RoleClaimType, role.Value))
                            identity.AddClaim(new System.Security.Claims.Claim(identity.RoleClaimType, role.Value));
                    }

                    // 2. Map Keycloak default "realm_access" (it's often a JSON string)
                    var realmAccessClaim = identity.FindFirst("realm_access");
                    if (realmAccessClaim != null)
                    {
                        try 
                        {
                            using var doc = System.Text.Json.JsonDocument.Parse(realmAccessClaim.Value);
                            if (doc.RootElement.TryGetProperty("roles", out var rolesElement))
                            {
                                foreach (var role in rolesElement.EnumerateArray())
                                {
                                    var roleValue = role.GetString();
                                    if (!string.IsNullOrEmpty(roleValue) && !identity.HasClaim(identity.RoleClaimType, roleValue))
                                    {
                                        identity.AddClaim(new System.Security.Claims.Claim(identity.RoleClaimType, roleValue));
                                    }
                                }
                            }
                        }
                        catch { /* skip if not valid JSON */ }
                    }
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    
    var context = scope.ServiceProvider.GetRequiredService<CanteenContext>();
    context.Database.EnsureCreated();
}

// MEALS 

app.MapGet("/api/meals", GetMeals);
app.MapGet("/api/meals/{id:int}", GetMeal);
app.MapPost("/api/meals", CreateMeal);
app.MapPut("/api/meals/{id:int}", UpdateMeal);
app.MapPatch("/api/meals/{id:int}/deactivate", DeactivateMeal);

// MENU ITEMS

app.MapGet("/api/menu", GetMenuItems);
app.MapGet("/api/menu/today", GetTodayMenu);
app.MapPost("/api/menu", CreateMenuItem);
app.MapPut("/api/menu/{id:int}", UpdateMenuItem);
app.MapDelete("/api/menu/{id:int}", DeleteMenuItem);

// SSE
app.MapGet("/api/notifications/sse", async (HttpContext ctx, [Microsoft.AspNetCore.Mvc.FromServices] SseNotifier notifier) =>
{
    ctx.Response.Headers.Append("Content-Type", "text/event-stream");
    
    var reader = notifier.Subscribe();
    
    try
    {
        await foreach (var message in reader.ReadAllAsync(ctx.RequestAborted))
        {
            await ctx.Response.WriteAsync($"data: {message}\n\n");
            await ctx.Response.Body.FlushAsync();
        }
    }
    catch (OperationCanceledException)
    {
        // client disconnected
    }
    finally
    {
        notifier.Unsubscribe(reader);
    }
});

// ORDERS

app.MapGet("/api/orders", GetOrders);
app.MapPost("/api/orders/batch", GetOrdersBatch); // Public for students
app.MapPost("/api/orders", CreateOrder); // Public for students
app.MapPut("/api/orders/{id:int}/status", (int id, UpdateOrderStatusDto dto, CanteenContext db, [Microsoft.AspNetCore.Mvc.FromServices] UTB.Minute.WebApi.Services.SseNotifier notifier, ILogger<Program> logger) => 
{
    return UpdateOrderStatus(id, dto, db, notifier);
});

app.UseHttpsRedirection();
app.Run();

// MEAL HANDLERS

static async Task<Ok<List<MealDto>>> GetMeals(CanteenContext db)
{
    var meals = await db.Meals
        .Select(m => new MealDto(m.Id, m.Description, m.Allergens, m.Price, m.IsActive))
        .ToListAsync();

    return TypedResults.Ok(meals);
}

static async Task<Results<Ok<MealDto>, NotFound>> GetMeal(int id, CanteenContext db)
{
    if (await db.Meals.FindAsync(id) is Meal meal)
    {
        return TypedResults.Ok(new MealDto(meal.Id, meal.Description, meal.Allergens, meal.Price, meal.IsActive));
    }

    return TypedResults.NotFound();
}

static async Task<Results<Created<MealDto>, BadRequest<string>>> CreateMeal(CreateMealDto dto, CanteenContext db)
{
    if (string.IsNullOrWhiteSpace(dto.Description))
        return TypedResults.BadRequest("Description is required.");

    if (dto.Price <= 0)
        return TypedResults.BadRequest("Price must be greater than zero.");

    var meal = new Meal { Description = dto.Description, Allergens = dto.Allergens, Price = dto.Price };

    db.Meals.Add(meal);
    await db.SaveChangesAsync();

    return TypedResults.Created($"/api/meals/{meal.Id}", new MealDto(meal.Id, meal.Description, meal.Allergens, meal.Price, meal.IsActive));
}

static async Task<Results<Ok<MealDto>, NotFound, BadRequest<string>>> UpdateMeal(int id, UpdateMealDto dto, CanteenContext db)
{
    if (string.IsNullOrWhiteSpace(dto.Description))
        return TypedResults.BadRequest("Description is required.");

    if (dto.Price <= 0)
        return TypedResults.BadRequest("Price must be greater than zero.");

    if (await db.Meals.FindAsync(id) is Meal meal)
    {
        meal.Description = dto.Description;
        meal.Allergens = dto.Allergens;
        meal.Price = dto.Price;
        meal.IsActive = dto.IsActive;

        await db.SaveChangesAsync();

        return TypedResults.Ok(new MealDto(meal.Id, meal.Description, meal.Allergens, meal.Price, meal.IsActive));
    }

    return TypedResults.NotFound();
}

static async Task<Results<NoContent, NotFound>> DeactivateMeal(int id, CanteenContext db)
{
    if (await db.Meals.FindAsync(id) is Meal meal)
    {
        meal.IsActive = false;
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    return TypedResults.NotFound();
}

// MENU ITEM HANDLERS

static async Task<Ok<List<MenuItemDto>>> GetMenuItems(CanteenContext db, DateOnly? date)
{
    var query = db.MenuItems.Include(mi => mi.Meal).AsQueryable();
    
    if (date.HasValue)
    {
        query = query.Where(mi => mi.Date == date.Value);
    }

    var items = await query
        .Select(mi => new MenuItemDto(mi.Id, mi.Date, mi.AvailablePortions, mi.MealId, mi.Meal.Description, mi.Meal.Allergens, mi.Meal.Price))
        .ToListAsync();

    return TypedResults.Ok(items);
}

static async Task<Ok<List<MenuItemDto>>> GetTodayMenu(CanteenContext db)
{
    var today = DateOnly.FromDateTime(DateTime.Today);

    var items = await db.MenuItems
        .Include(mi => mi.Meal)
        .Where(mi => mi.Date == today)
        .Select(mi => new MenuItemDto(mi.Id, mi.Date, mi.AvailablePortions, mi.MealId, mi.Meal.Description, mi.Meal.Allergens, mi.Meal.Price))
        .ToListAsync();

    return TypedResults.Ok(items);
}

static async Task<Results<Created<MenuItemDto>, BadRequest<string>>> CreateMenuItem(CreateMenuItemDto dto, CanteenContext db)
{
    if (dto.AvailablePortions < 0)
        return TypedResults.BadRequest("AvailablePortions cannot be negative.");

    var menuItem = new MenuItem { Date = dto.Date, AvailablePortions = dto.AvailablePortions, MealId = dto.MealId };

    db.MenuItems.Add(menuItem);
    await db.SaveChangesAsync();

    var created = await db.MenuItems.Include(mi => mi.Meal).FirstAsync(mi => mi.Id == menuItem.Id);

    return TypedResults.Created($"/api/menu/{menuItem.Id}",
        new MenuItemDto(created.Id, created.Date, created.AvailablePortions, created.MealId, created.Meal.Description, created.Meal.Allergens, created.Meal.Price));
}

static async Task<Results<Ok<MenuItemDto>, NotFound, BadRequest<string>>> UpdateMenuItem(int id, UpdateMenuItemDto dto, CanteenContext db)
{
    var menuItem = await db.MenuItems.Include(mi => mi.Meal).FirstOrDefaultAsync(mi => mi.Id == id);

    if (dto.AvailablePortions < 0)
        return TypedResults.BadRequest("AvailablePortions cannot be negative.");

    if (menuItem is not null)
    {
        menuItem.Date = dto.Date;
        menuItem.AvailablePortions = dto.AvailablePortions;

        await db.SaveChangesAsync();

        return TypedResults.Ok(new MenuItemDto(menuItem.Id, menuItem.Date, menuItem.AvailablePortions, menuItem.MealId, menuItem.Meal.Description, menuItem.Meal.Allergens, menuItem.Meal.Price));
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
        .Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled)
        .Select(o => new OrderDto(o.Id, o.Status.ToString(), o.CreatedAt, o.MenuItemId, o.MenuItem.Meal.Description, o.MenuItem.Meal.Allergens))
        .ToListAsync();

    return TypedResults.Ok(orders);
}

static async Task<Ok<List<OrderDto>>> GetOrdersBatch([Microsoft.AspNetCore.Mvc.FromBody] List<int> ids, CanteenContext db)
{
    var orders = await db.Orders
        .Include(o => o.MenuItem)
        .ThenInclude(mi => mi.Meal)
        .Where(o => ids.Contains(o.Id))
        .Select(o => new OrderDto(o.Id, o.Status.ToString(), o.CreatedAt, o.MenuItemId, o.MenuItem.Meal.Description, o.MenuItem.Meal.Allergens))
        .ToListAsync();

    return TypedResults.Ok(orders);
}

static async Task<Results<Created<OrderDto>, NotFound, BadRequest<string>>> CreateOrder(CreateOrderDto dto, CanteenContext db, [Microsoft.AspNetCore.Mvc.FromServices] UTB.Minute.WebApi.Services.SseNotifier notifier, ILogger<Program> logger)
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

    try
    {
        await db.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException)
    {
        return TypedResults.BadRequest("Niekto iný si práve objednal túto porciu. Skúste to znova.");
    }

    await notifier.NotifyAsync("OrderCreated");
    
    logger.LogInformation("Order created: {OrderId} for MenuItem {MenuItemId}", order.Id, order.MenuItemId);

    return TypedResults.Created($"/api/orders/{order.Id}",
        new OrderDto(order.Id, order.Status.ToString(), order.CreatedAt, order.MenuItemId, menuItem.Meal.Description, menuItem.Meal.Allergens));
}

static async Task<Results<Ok<OrderDto>, NotFound, BadRequest<string>>> UpdateOrderStatus(int id, UpdateOrderStatusDto dto, CanteenContext db, [Microsoft.AspNetCore.Mvc.FromServices] UTB.Minute.WebApi.Services.SseNotifier notifier)
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

    await notifier.NotifyAsync("OrderUpdated");

    return TypedResults.Ok(new OrderDto(order.Id, order.Status.ToString(), order.CreatedAt, order.MenuItemId, order.MenuItem.Meal.Description, order.MenuItem.Meal.Allergens));
}

public partial class Program { }
