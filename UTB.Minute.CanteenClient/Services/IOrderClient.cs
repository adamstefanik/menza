using UTB.Minute.Contracts;

namespace UTB.Minute.CanteenClient.Services;

public interface IOrderClient
{
    Task<IEnumerable<MenuItemDto>> GetTodayMenuAsync();
    Task<IEnumerable<MenuItemDto>> GetMenuByDateAsync(DateOnly date);
    Task<OrderDto> CreateOrderAsync(CreateOrderDto dto);
    Task<IEnumerable<OrderDto>> GetOrdersBatchAsync(List<int> ids);
}
