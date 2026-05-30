using UTB.Minute.Contracts;

namespace UTB.Minute.AdminClient.Services;

public interface IMealClient
{
    Task<IEnumerable<MealDto>> GetMealsAsync();
    Task<MealDto?> GetMealAsync(int id);
    Task CreateMealAsync(MealDto meal);
    Task UpdateMealAsync(int id, MealDto meal);
    Task DeactivateMealAsync(int id);
}