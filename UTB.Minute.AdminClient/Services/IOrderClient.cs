using UTB.Minute.Contracts;

namespace UTB.Minute.AdminClient.Services;

public interface IOrderClient
{
    Task<IEnumerable<OrderDto>> GetOrdersAsync();
    Task UpdateOrderStatusAsync(int id, UpdateOrderStatusDto dto);
}