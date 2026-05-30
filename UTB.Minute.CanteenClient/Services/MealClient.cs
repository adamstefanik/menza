using System.Net.Http.Json;
using UTB.Minute.Contracts;

namespace UTB.Minute.CanteenClient.Services;

public class MealClient(HttpClient httpClient) : IMealClient
{
    public async Task<IEnumerable<MealDto>> GetMealsAsync() => 
        await httpClient.GetFromJsonAsync<IEnumerable<MealDto>>("meals") ?? [];

    public async Task CreateMealAsync(MealDto meal) => 
        await httpClient.PostAsJsonAsync("meals", meal);

    public async Task UpdateMealAsync(int id, MealDto meal) => 
        await httpClient.PutAsJsonAsync($"meals/{id}", meal);

    public async Task DeleteMealAsync(int id) => 
        await httpClient.DeleteAsync($"meals/{id}");
}