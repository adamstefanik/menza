using System.Net.Http.Json;
using UTB.Minute.Contracts;

namespace UTB.Minute.AdminClient.Services;

public class MealClient(HttpClient httpClient) : IMealClient
{
    public async Task<IEnumerable<MealDto>> GetMealsAsync() 
    {
        try 
        {
            var response = await httpClient.GetAsync("api/meals");
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"API Error: {response.StatusCode}");
                return [];
            }

            return await response.Content.ReadFromJsonAsync<IEnumerable<MealDto>>() ?? [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection Error: {ex.Message}");
            return [];
        }
    }

    public async Task<MealDto?> GetMealAsync(int id)
    {
        try 
        {
            return await httpClient.GetFromJsonAsync<MealDto>($"api/meals/{id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading meal {id}: {ex.Message}");
            return null;
        }
    }

    public async Task CreateMealAsync(MealDto meal)
    {
        var dto = new { Description = meal.Description, Price = meal.Price };
        var response = await httpClient.PostAsJsonAsync("api/meals", dto);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"API Error {response.StatusCode}: {errorContent}");
        }
    }

    public async Task UpdateMealAsync(int id, MealDto meal) =>
        await httpClient.PutAsJsonAsync($"api/meals/{id}", meal);

    public async Task DeactivateMealAsync(int id)
    {
        var response = await httpClient.PatchAsync($"api/meals/{id}/deactivate", null);
        try { response.EnsureSuccessStatusCode(); } catch { /* fail silently */ }
    }
}
