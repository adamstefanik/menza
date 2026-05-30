namespace UTB.Minute.Contracts;

public record MenuItemDto(int Id, DateOnly Date, int AvailablePortions, int MealId, string MealDescription, string? Allergens, decimal Price);
public record CreateMenuItemDto(DateOnly Date, int AvailablePortions, int MealId);
public record UpdateMenuItemDto(DateOnly Date, int AvailablePortions);
