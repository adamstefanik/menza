using System.Net.Http.Json;
using System.Net.Http.Headers;
using UTB.Minute.Contracts;

namespace UTB.Minute.AdminClient.Services;

public class OrderClient(HttpClient httpClient) : IOrderClient
{
    public async Task<IEnumerable<OrderDto>> GetOrdersAsync()
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

    public async Task UpdateOrderStatusAsync(int id, UpdateOrderStatusDto dto)
    {
        var response = await httpClient.PutAsJsonAsync($"api/orders/{id}/status", dto);
        response.EnsureSuccessStatusCode();
    }
}
