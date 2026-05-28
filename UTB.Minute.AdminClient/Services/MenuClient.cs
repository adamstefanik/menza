using System.Net.Http.Json;
using UTB.Minute.Contracts;

namespace UTB.Minute.AdminClient.Services;

public class MenuClient(HttpClient httpClient) : IMenuClient
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

    public async Task CreateMenuItemAsync(CreateMenuItemDto dto)
    {
        var response = await httpClient.PostAsJsonAsync("api/menu", dto);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"API Error: {error}");
        }
    }

    public async Task UpdateMenuItemAsync(int id, UpdateMenuItemDto dto)
    {
        await httpClient.PutAsJsonAsync($"api/menu/{id}", dto);
    }

    public async Task DeleteMenuItemAsync(int id)
    {
        await httpClient.DeleteAsync($"api/menu/{id}");
    }
    
}