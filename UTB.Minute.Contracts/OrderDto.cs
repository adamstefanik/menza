namespace UTB.Minute.Contracts;

public record OrderDto(int Id, string Status, DateTime CreatedAt, int MenuItemId, string MealDescription);
public record CreateOrderDto(int MenuItemId);
public record UpdateOrderStatusDto(string Status);