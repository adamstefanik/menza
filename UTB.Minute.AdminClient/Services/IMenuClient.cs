using UTB.Minute.Contracts;

namespace UTB.Minute.AdminClient.Services;

public interface IMenuClient
{
    Task<IEnumerable<MenuItemDto>> GetMenuItemsAsync();
    Task<IEnumerable<MenuItemDto>> GetTodayMenuAsync();
    Task CreateMenuItemAsync(CreateMenuItemDto dto);
    Task UpdateMenuItemAsync(int id, UpdateMenuItemDto dto);
    Task DeleteMenuItemAsync(int id);
}