using System.Net.Http.Json;
using UTB.Minute.Contracts;

namespace UTB.Minute.CanteenClient.Services;

public class OrderClient(HttpClient httpClient) : IOrderClient
{
    public async Task<IEnumerable<MenuItemDto>> GetMenuItemsAsync()
    {
        try
        {
            return await httpClient.GetFromJsonAsync<IEnumerable<MenuItemDto>>("api/menu") ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return [];
        }
    }

    public async Task<IEnumerable<MenuItemDto>> GetTodayMenuAsync()
    {
        try
        {
            return await httpClient.GetFromJsonAsync<IEnumerable<MenuItemDto>>("api/menu/today") ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return [];
        }
    }

    public async Task<IEnumerable<MenuItemDto>> GetMenuByDateAsync(DateOnly date)
    {
        try
        {
            var dateStr = date.ToString("yyyy-MM-dd");
            return await httpClient.GetFromJsonAsync<IEnumerable<MenuItemDto>>($"api/menu?date={dateStr}") ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return [];
        }
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto)
    {
        var response = await httpClient.PostAsJsonAsync("api/orders", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderDto>() 
            ?? throw new Exception("Invalid response");
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersBatchAsync(List<int> ids)
    {
        try
        {
            if (ids == null || !ids.Any()) return [];
            var response = await httpClient.PostAsJsonAsync("api/orders/batch", ids);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IEnumerable<OrderDto>>() ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return [];
        }
    }
}
