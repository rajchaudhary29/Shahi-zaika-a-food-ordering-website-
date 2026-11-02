namespace ShahiZayka.Application.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsVegetarian { get; set; }
    public string SpiceLevel { get; set; } = string.Empty;
    public int PreparationMinutes { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int StockQuantity { get; set; } = 50;
    public bool IsAvailable { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
    public bool IsVegetarian { get; set; } = false;
    public string SpiceLevel { get; set; } = "Medium";
    public int PreparationMinutes { get; set; } = 25;
}

public class UpdateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int StockQuantity { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsVegetarian { get; set; }
    public string SpiceLevel { get; set; } = string.Empty;
    public int PreparationMinutes { get; set; }
}

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public int ProductCount { get; set; }
}
