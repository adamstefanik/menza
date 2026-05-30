using Microsoft.EntityFrameworkCore;

namespace UTB.Minute.Db;

public class CanteenContext : DbContext
{
    public CanteenContext(DbContextOptions<CanteenContext> options) : base(options) { }

    // Tieto názvy (Meals, MenuItems, Orders) musia sedieť s tým, čo máš v Program.cs
    public DbSet<Meal> Meals { get; set; } 
    public DbSet<MenuItem> MenuItems { get; set; } 
    public DbSet<Order> Orders { get; set; } 

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Povieme EF, že Meal patrí do tabuľky Meals
        modelBuilder.Entity<Meal>().ToTable("Meals");
        
        // Povieme EF, že MenuItem patrí do tabuľky MenuItems
        modelBuilder.Entity<MenuItem>().ToTable("MenuItems");
    }
}