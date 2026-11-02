namespace ShahiZayka.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    
    public int StockQuantity { get; set; } = 100;
    public bool IsAvailable { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
    public bool IsVegetarian { get; set; } = false;
    public string SpiceLevel { get; set; } = "Medium"; // Mild, Medium, Hot, Royal Spiced
    public int PreparationMinutes { get; set; } = 25;
    public double AverageRating { get; set; } = 4.8;
    public int ReviewCount { get; set; } = 120;
}
