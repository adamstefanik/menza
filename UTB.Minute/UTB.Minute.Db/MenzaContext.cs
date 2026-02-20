using Microsoft.EntityFrameworkCore;

namespace UTB.Minute.Db;

public class CanteenContext(DbContextOptions<CanteenContext> options) : DbContext(options)
{
    public DbSet<Meal> Meals { get; set; }
    public DbSet<MenuItem> MenuItems { get; set; }
    public DbSet<Order> Orders { get; set; }
}