namespace UTB.Minute.Contracts;

public record MealDto(int Id, string Description, decimal Price, bool IsActive);
public record CreateMealDto(string Description, decimal Price);
public record UpdateMealDto(string Description, decimal Price, bool IsActive);