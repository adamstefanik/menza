using UTB.Minute.Contracts;

namespace UTB.Minute.AdminClient.Services;

public interface IMealClient
{
    Task<IEnumerable<MealDto>> GetMealsAsync();
    Task CreateMealAsync(MealDto meal);
    Task UpdateMealAsync(int id, MealDto meal);
    Task DeleteMealAsync(int id);
}