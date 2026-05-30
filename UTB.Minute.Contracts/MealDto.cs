namespace UTB.Minute.Contracts;

public record MealDto(int Id, string Description, string? Allergens, decimal Price, bool IsActive);
public record CreateMealDto(string Description, string? Allergens, decimal Price);
public record UpdateMealDto(string Description, string? Allergens, decimal Price, bool IsActive);
