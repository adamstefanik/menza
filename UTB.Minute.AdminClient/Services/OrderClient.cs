using System.Net.Http.Json;
using UTB.Minute.Contracts;

namespace UTB.Minute.AdminClient.Services;

public class OrderClient(HttpClient httpClient) : IOrderClient
{
    public async Task<IEnumerable<OrderDto>> GetOrdersAsync()
    {
        try
        {
            var response = await httpClient.GetAsync("api/orders");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[API Error] GetOrdersAsync failed: {response.StatusCode}");
                return [];
            }
            return await response.Content.ReadFromJsonAsync<IEnumerable<OrderDto>>() ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return [];
        }
    }

    public async Task UpdateOrderStatusAsync(int id, UpdateOrderStatusDto dto)
    {
        var response = await httpClient.PutAsJsonAsync($"api/orders/{id}/status", dto);
        try { response.EnsureSuccessStatusCode(); } catch { /* fail silently */ }
    }
}
