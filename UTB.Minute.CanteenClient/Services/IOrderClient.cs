using UTB.Minute.Contracts;

namespace UTB.Minute.CanteenClient.Services;

public interface IOrderClient
{
    Task<IEnumerable<MenuItemDto>> GetTodayMenuAsync();
    Task<OrderDto> CreateOrderAsync(int menuItemId);
    Task<IEnumerable<OrderDto>> GetMyOrdersAsync();
}