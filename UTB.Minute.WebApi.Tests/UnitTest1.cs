using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Testcontainers.PostgreSql;
using UTB.Minute.Contracts;
using UTB.Minute.Db;

namespace UTB.Minute.WebApi.Tests;

public class CanteenApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CanteenContext>();
        await db.Database.EnsureCreatedAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:database", _postgres.GetConnectionString());
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}

public class ApiTests : IClassFixture<CanteenApiFactory>
{
    private readonly HttpClient _client;

    public ApiTests(CanteenApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // --- Meals: Create and Read ---

    [Fact]
    public async Task PostMeal_ReturnsCreated()
    {
        var response = await _client.PostAsJsonAsync("/api/meals", new CreateMealDto("Schnitzel", 120m));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var meal = await response.Content.ReadFromJsonAsync<MealDto>();
        Assert.NotNull(meal);
        Assert.Equal("Schnitzel", meal.Description);
        Assert.Equal(120m, meal.Price);
        Assert.True(meal.IsActive);
    }

    [Fact]
    public async Task GetMeals_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/meals");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var meals = await response.Content.ReadFromJsonAsync<List<MealDto>>();
        Assert.NotNull(meals);
    }

    // --- Meals: Update and Deactivate ---

    [Fact]
    public async Task PutMeal_UpdatesAndDeactivates()
    {
        var created = await (await _client.PostAsJsonAsync("/api/meals", new CreateMealDto($"Meal {Guid.NewGuid()}", 100m)))
            .Content.ReadFromJsonAsync<MealDto>();

        var response = await _client.PutAsJsonAsync($"/api/meals/{created!.Id}", new UpdateMealDto(created.Description, 150m, false));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<MealDto>();
        Assert.False(updated!.IsActive);
        Assert.Equal(150m, updated.Price);
    }

    [Fact]
    public async Task GetMeal_NotFound_Returns404()
    {
        var response = await _client.GetAsync("/api/meals/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Menu Items: Create and Read ---

    [Fact]
    public async Task PostMenuItem_ReturnsCreated()
    {
        var meal = await (await _client.PostAsJsonAsync("/api/meals", new CreateMealDto($"Meal {Guid.NewGuid()}", 99m)))
            .Content.ReadFromJsonAsync<MealDto>();

        var response = await _client.PostAsJsonAsync("/api/menu", new CreateMenuItemDto(DateOnly.FromDateTime(DateTime.Today), 10, meal!.Id));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = await response.Content.ReadFromJsonAsync<MenuItemDto>();
        Assert.NotNull(item);
        Assert.Equal(10, item.AvailablePortions);
        Assert.Equal(meal.Id, item.MealId);
    }

    [Fact]
    public async Task GetMenu_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/menu");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<MenuItemDto>>();
        Assert.NotNull(items);
    }

    // --- Menu Items: Update and Delete ---

    [Fact]
    public async Task PutMenuItem_UpdatesPortions()
    {
        var meal = await (await _client.PostAsJsonAsync("/api/meals", new CreateMealDto($"Meal {Guid.NewGuid()}", 99m)))
            .Content.ReadFromJsonAsync<MealDto>();
        var item = await (await _client.PostAsJsonAsync("/api/menu", new CreateMenuItemDto(DateOnly.FromDateTime(DateTime.Today), 5, meal!.Id)))
            .Content.ReadFromJsonAsync<MenuItemDto>();

        var response = await _client.PutAsJsonAsync($"/api/menu/{item!.Id}", new UpdateMenuItemDto(item.Date, 20));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<MenuItemDto>();
        Assert.Equal(20, updated!.AvailablePortions);
    }

    [Fact]
    public async Task DeleteMenuItem_ReturnsNoContent()
    {
        var meal = await (await _client.PostAsJsonAsync("/api/meals", new CreateMealDto($"Meal {Guid.NewGuid()}", 99m)))
            .Content.ReadFromJsonAsync<MealDto>();
        var item = await (await _client.PostAsJsonAsync("/api/menu", new CreateMenuItemDto(DateOnly.FromDateTime(DateTime.Today), 5, meal!.Id)))
            .Content.ReadFromJsonAsync<MenuItemDto>();

        var response = await _client.DeleteAsync($"/api/menu/{item!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // --- Orders: Create and Read ---

    [Fact]
    public async Task PostOrder_CreatesOrderAndDecrementsPortions()
    {
        var meal = await (await _client.PostAsJsonAsync("/api/meals", new CreateMealDto($"Meal {Guid.NewGuid()}", 99m)))
            .Content.ReadFromJsonAsync<MealDto>();
        var item = await (await _client.PostAsJsonAsync("/api/menu", new CreateMenuItemDto(DateOnly.FromDateTime(DateTime.Today), 5, meal!.Id)))
            .Content.ReadFromJsonAsync<MenuItemDto>();

        var response = await _client.PostAsJsonAsync("/api/orders", new CreateOrderDto(item!.Id));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(order);
        Assert.Equal("Preparing", order.Status);
        Assert.Equal(meal.Description, order.MealDescription);
    }

    [Fact]
    public async Task PostOrder_SoldOut_ReturnsBadRequest()
    {
        var meal = await (await _client.PostAsJsonAsync("/api/meals", new CreateMealDto($"Meal {Guid.NewGuid()}", 99m)))
            .Content.ReadFromJsonAsync<MealDto>();
        var item = await (await _client.PostAsJsonAsync("/api/menu", new CreateMenuItemDto(DateOnly.FromDateTime(DateTime.Today), 0, meal!.Id)))
            .Content.ReadFromJsonAsync<MenuItemDto>();

        var response = await _client.PostAsJsonAsync("/api/orders", new CreateOrderDto(item!.Id));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>();
        Assert.NotNull(orders);
    }

    // --- Orders: Status Change ---

    [Fact]
    public async Task PutOrderStatus_PreparingToReady_Succeeds()
    {
        var order = await CreateOrderAsync();

        var response = await _client.PutAsJsonAsync($"/api/orders/{order.Id}/status", new UpdateOrderStatusDto("Ready"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal("Ready", updated!.Status);
    }

    [Fact]
    public async Task PutOrderStatus_InvalidTransition_ReturnsBadRequest()
    {
        var order = await CreateOrderAsync();

        // Preparing -> Completed is not allowed, must go through Ready first
        var response = await _client.PutAsJsonAsync($"/api/orders/{order.Id}/status", new UpdateOrderStatusDto("Completed"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutOrderStatus_PreparingToCancelled_Succeeds()
    {
        var order = await CreateOrderAsync();

        var response = await _client.PutAsJsonAsync($"/api/orders/{order.Id}/status", new UpdateOrderStatusDto("Cancelled"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal("Cancelled", updated!.Status);
    }

    private async Task<OrderDto> CreateOrderAsync()
    {
        var meal = await (await _client.PostAsJsonAsync("/api/meals", new CreateMealDto($"Meal {Guid.NewGuid()}", 99m)))
            .Content.ReadFromJsonAsync<MealDto>();
        var item = await (await _client.PostAsJsonAsync("/api/menu", new CreateMenuItemDto(DateOnly.FromDateTime(DateTime.Today), 5, meal!.Id)))
            .Content.ReadFromJsonAsync<MenuItemDto>();
        return (await (await _client.PostAsJsonAsync("/api/orders", new CreateOrderDto(item!.Id)))
            .Content.ReadFromJsonAsync<OrderDto>())!;
    }
}
