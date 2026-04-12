using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using UTB.Minute.Contracts;
using UTB.Minute.Db;

namespace UTB.Minute.WebApi.Tests;

public class TestFixture : IAsyncLifetime
{
    private DistributedApplication app = null!;
    private string? connectionString;
    public HttpClient HttpClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.UTB_Minute_AppHost>(["--environment=Testing"]);

        app = await builder.BuildAsync();

        await app.StartAsync();

        await app.ResourceNotifications.WaitForResourceHealthyAsync("database");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("webapi");

        connectionString = await app.GetConnectionStringAsync("database");
        HttpClient = app.CreateHttpClient("webapi", "http");

        using var context = CreateContext();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        var meal1 = new Meal { Description = "Chicken schnitzel", Price = 120 };
        var meal2 = new Meal { Description = "Beef goulash", Price = 135 };

        context.Meals.AddRange(meal1, meal2);
        await context.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);

        var menuItem1 = new MenuItem { Date = today, AvailablePortions = 5, MealId = meal1.Id };
        var menuItem2 = new MenuItem { Date = today, AvailablePortions = 0, MealId = meal2.Id };

        context.MenuItems.AddRange(menuItem1, menuItem2);
        await context.SaveChangesAsync();
    }

    public CanteenContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CanteenContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new CanteenContext(options);
    }

    public async Task DisposeAsync()
    {
        HttpClient.Dispose();
        await app.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}

[CollectionDefinition("Database collection", DisableParallelization = true)]
public class DatabaseCollection : ICollectionFixture<TestFixture>
{
}

[Collection("Database collection")]
public class CanteenApiTests(TestFixture fixture)
{
    // Meals: Create and Read

    [Fact]
    public async Task PostMeal_ReturnsCreatedAndPersistsMeal()
    {
        var response = await fixture.HttpClient.PostAsJsonAsync(
            "/api/meals",
            new CreateMealDto("Schnitzel", 120m));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        MealDto? meal = await response.Content.ReadFromJsonAsync<MealDto>();

        Assert.NotNull(meal);
        Assert.Equal("Schnitzel", meal.Description);
        Assert.Equal(120m, meal.Price);
        Assert.True(meal.IsActive);

        using var context = fixture.CreateContext();

        Meal? persisted = await context.Meals.FindAsync(meal.Id);

        Assert.NotNull(persisted);
        Assert.Equal("Schnitzel", persisted.Description);
    }

    [Fact]
    public async Task GetMeals_ReturnsSeededMeals()
    {
        var response = await fixture.HttpClient.GetAsync("/api/meals");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        List<MealDto>? meals = await response.Content.ReadFromJsonAsync<List<MealDto>>();

        Assert.NotNull(meals);
        Assert.Contains(meals, m => m.Description == "Chicken schnitzel");
        Assert.Contains(meals, m => m.Description == "Beef goulash");
    }

    // Meals: Update and Deactivate

    [Fact]
    public async Task PutMeal_UpdatesAndDeactivates()
    {
        var created = await (await fixture.HttpClient.PostAsJsonAsync(
            "/api/meals",
            new CreateMealDto($"Meal {Guid.NewGuid()}", 100m)))
            .Content.ReadFromJsonAsync<MealDto>();

        var response = await fixture.HttpClient.PutAsJsonAsync(
            $"/api/meals/{created!.Id}",
            new UpdateMealDto(created.Description, 150m, false));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        MealDto? updated = await response.Content.ReadFromJsonAsync<MealDto>();

        Assert.NotNull(updated);
        Assert.False(updated.IsActive);
        Assert.Equal(150m, updated.Price);

        using var context = fixture.CreateContext();

        Meal? persisted = await context.Meals.FindAsync(created.Id);

        Assert.NotNull(persisted);
        Assert.False(persisted.IsActive);
        Assert.Equal(150m, persisted.Price);
    }

    [Fact]
    public async Task PatchMealDeactivate_DeactivatesMeal()
    {
        var created = await (await fixture.HttpClient.PostAsJsonAsync(
            "/api/meals",
            new CreateMealDto($"Meal {Guid.NewGuid()}", 100m)))
            .Content.ReadFromJsonAsync<MealDto>();

        var response = await fixture.HttpClient.PatchAsync($"/api/meals/{created!.Id}/deactivate", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var context = fixture.CreateContext();

        Meal? persisted = await context.Meals.FindAsync(created.Id);

        Assert.NotNull(persisted);
        Assert.False(persisted.IsActive);
    }

    [Fact]
    public async Task GetMeal_ReturnsNotFound_WhenMealDoesNotExist()
    {
        var response = await fixture.HttpClient.GetAsync("/api/meals/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Menu Items: Create and Read

    [Fact]
    public async Task PostMenuItem_ReturnsCreatedAndPersistsMenuItem()
    {
        var meal = await (await fixture.HttpClient.PostAsJsonAsync(
            "/api/meals",
            new CreateMealDto($"Meal {Guid.NewGuid()}", 99m)))
            .Content.ReadFromJsonAsync<MealDto>();

        var response = await fixture.HttpClient.PostAsJsonAsync(
            "/api/menu",
            new CreateMenuItemDto(DateOnly.FromDateTime(DateTime.Today), 10, meal!.Id));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        MenuItemDto? item = await response.Content.ReadFromJsonAsync<MenuItemDto>();

        Assert.NotNull(item);
        Assert.Equal(10, item.AvailablePortions);
        Assert.Equal(meal.Id, item.MealId);

        using var context = fixture.CreateContext();

        MenuItem? persisted = await context.MenuItems.FindAsync(item.Id);

        Assert.NotNull(persisted);
        Assert.Equal(10, persisted.AvailablePortions);
    }

    [Fact]
    public async Task GetMenu_ReturnsOk()
    {
        var response = await fixture.HttpClient.GetAsync("/api/menu");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        List<MenuItemDto>? items = await response.Content.ReadFromJsonAsync<List<MenuItemDto>>();

        Assert.NotNull(items);
    }

    // Menu Items: Update and Delete

    [Fact]
    public async Task PutMenuItem_UpdatesPortions()
    {
        var meal = await (await fixture.HttpClient.PostAsJsonAsync(
            "/api/meals",
            new CreateMealDto($"Meal {Guid.NewGuid()}", 99m)))
            .Content.ReadFromJsonAsync<MealDto>();

        var item = await (await fixture.HttpClient.PostAsJsonAsync(
            "/api/menu",
            new CreateMenuItemDto(DateOnly.FromDateTime(DateTime.Today), 5, meal!.Id)))
            .Content.ReadFromJsonAsync<MenuItemDto>();

        var response = await fixture.HttpClient.PutAsJsonAsync(
            $"/api/menu/{item!.Id}",
            new UpdateMenuItemDto(item.Date, 20));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        MenuItemDto? updated = await response.Content.ReadFromJsonAsync<MenuItemDto>();

        Assert.NotNull(updated);
        Assert.Equal(20, updated.AvailablePortions);

        using var context = fixture.CreateContext();

        MenuItem? persisted = await context.MenuItems.FindAsync(item.Id);

        Assert.NotNull(persisted);
        Assert.Equal(20, persisted.AvailablePortions);
    }

    [Fact]
    public async Task DeleteMenuItem_DeletesMenuItem_WhenExists()
    {
        var meal = await (await fixture.HttpClient.PostAsJsonAsync(
            "/api/meals",
            new CreateMealDto($"Meal {Guid.NewGuid()}", 99m)))
            .Content.ReadFromJsonAsync<MealDto>();

        var item = await (await fixture.HttpClient.PostAsJsonAsync(
            "/api/menu",
            new CreateMenuItemDto(DateOnly.FromDateTime(DateTime.Today), 5, meal!.Id)))
            .Content.ReadFromJsonAsync<MenuItemDto>();

        var response = await fixture.HttpClient.DeleteAsync($"/api/menu/{item!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var context = fixture.CreateContext();

        MenuItem? deleted = await context.MenuItems.FindAsync(item.Id);

        Assert.Null(deleted);
    }

    // Orders: Create and Read

    [Fact]
    public async Task PostOrder_CreatesOrderAndDecrementsPortions()
    {
        var meal = await (await fixture.HttpClient.PostAsJsonAsync(
            "/api/meals",
            new CreateMealDto($"Meal {Guid.NewGuid()}", 99m)))
            .Content.ReadFromJsonAsync<MealDto>();

        var item = await (await fixture.HttpClient.PostAsJsonAsync(
            "/api/menu",
            new CreateMenuItemDto(DateOnly.FromDateTime(DateTime.Today), 5, meal!.Id)))
            .Content.ReadFromJsonAsync<MenuItemDto>();

        var response = await fixture.HttpClient.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderDto(item!.Id));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        OrderDto? order = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.NotNull(order);
        Assert.Equal("Preparing", order.Status);
        Assert.Equal(meal.Description, order.MealDescription);

        using var context = fixture.CreateContext();

        MenuItem? updatedItem = await context.MenuItems.FindAsync(item.Id);

        Assert.NotNull(updatedItem);
        Assert.Equal(4, updatedItem.AvailablePortions);
    }

    [Fact]
    public async Task PostOrder_SoldOut_ReturnsBadRequest()
    {
        var meal = await (await fixture.HttpClient.PostAsJsonAsync(
            "/api/meals",
            new CreateMealDto($"Meal {Guid.NewGuid()}", 99m)))
            .Content.ReadFromJsonAsync<MealDto>();

        var item = await (await fixture.HttpClient.PostAsJsonAsync(
            "/api/menu",
            new CreateMenuItemDto(DateOnly.FromDateTime(DateTime.Today), 0, meal!.Id)))
            .Content.ReadFromJsonAsync<MenuItemDto>();

        var response = await fixture.HttpClient.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderDto(item!.Id));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_ReturnsOk()
    {
        var response = await fixture.HttpClient.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        List<OrderDto>? orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>();

        Assert.NotNull(orders);
    }

    // Orders: Status Change

    [Fact]
    public async Task PutOrderStatus_PreparingToReady_Succeeds()
    {
        OrderDto order = await CreateOrderAsync();

        var response = await fixture.HttpClient.PutAsJsonAsync(
            $"/api/orders/{order.Id}/status",
            new UpdateOrderStatusDto("Ready"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        OrderDto? updated = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal("Ready", updated!.Status);
    }

    [Fact]
    public async Task PutOrderStatus_InvalidTransition_ReturnsBadRequest()
    {
        OrderDto order = await CreateOrderAsync();

        var response = await fixture.HttpClient.PutAsJsonAsync(
            $"/api/orders/{order.Id}/status",
            new UpdateOrderStatusDto("Completed"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutOrderStatus_PreparingToCancelled_Succeeds()
    {
        OrderDto order = await CreateOrderAsync();

        var response = await fixture.HttpClient.PutAsJsonAsync(
            $"/api/orders/{order.Id}/status",
            new UpdateOrderStatusDto("Cancelled"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        OrderDto? updated = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal("Cancelled", updated!.Status);
    }

    private async Task<OrderDto> CreateOrderAsync()
    {
        var meal = await (await fixture.HttpClient.PostAsJsonAsync(
            "/api/meals",
            new CreateMealDto($"Meal {Guid.NewGuid()}", 99m)))
            .Content.ReadFromJsonAsync<MealDto>();

        var item = await (await fixture.HttpClient.PostAsJsonAsync(
            "/api/menu",
            new CreateMenuItemDto(DateOnly.FromDateTime(DateTime.Today), 5, meal!.Id)))
            .Content.ReadFromJsonAsync<MenuItemDto>();

        return (await (await fixture.HttpClient.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderDto(item!.Id)))
            .Content.ReadFromJsonAsync<OrderDto>())!;
    }
}
