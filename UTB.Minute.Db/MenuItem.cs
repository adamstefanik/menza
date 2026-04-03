namespace UTB.Minute.Db;

public class MenuItem
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public int AvailablePortions { get; set; }

    public int MealId { get; set; }
    public Meal Meal { get; set; } = null!;

    public List<Order> Orders { get; set; } = [];
}