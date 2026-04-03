namespace UTB.Minute.Db;

public class Meal
{
    public int Id { get; set; }
    public required string Description { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;

    public List<MenuItem> MenuItems { get; set; } = [];
}