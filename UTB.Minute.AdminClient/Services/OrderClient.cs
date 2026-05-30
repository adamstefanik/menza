using System.Net.Http.Json;
using System.Net.Http.Headers;
using UTB.Minute.Contracts;

namespace UTB.Minute.AdminClient.Services;

public class OrderClient(HttpClient httpClient, TokenProvider tokenProvider) : IOrderClient
{
    public async Task<IEnumerable<OrderDto>> GetOrdersAsync()
    {
        try
        {
            AttachToken();
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
        AttachToken();
        var response = await httpClient.PutAsJsonAsync($"api/orders/{id}/status", dto);
        response.EnsureSuccessStatusCode();
    }

    private void AttachToken()
    {
        if (!string.IsNullOrEmpty(tokenProvider.AccessToken))
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenProvider.AccessToken);
        }
    }
}
