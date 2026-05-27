using System.Net.Http.Json;
using UTB.Minute.Contracts;

namespace UTB.Minute.CanteenClient.Services;

public class OrderClient(HttpClient httpClient) : IOrderClient
{
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

    public async Task<OrderDto> CreateOrderAsync(int menuItemId)
    {
        var dto = new CreateOrderDto(menuItemId);
        var response = await httpClient.PostAsJsonAsync("api/orders", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderDto>() 
            ?? throw new Exception("Invalid response");
    }

    public async Task<IEnumerable<OrderDto>> GetMyOrdersAsync()
    {
        try
        {
            return await httpClient.GetFromJsonAsync<IEnumerable<OrderDto>>("api/orders") ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return [];
        }
    }
}