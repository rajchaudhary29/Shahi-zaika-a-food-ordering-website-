using Microsoft.EntityFrameworkCore;
using ShahiZayka.Application.Interfaces;
using ShahiZayka.Domain.Entities;
using ShahiZayka.Domain.Enums;

namespace ShahiZayka.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ShahiZaykaDbContext context, IPasswordHasher passwordHasher)
    {
        // Ensure database exists
        await context.Database.EnsureCreatedAsync();

        // 1. Seed Categories if empty
        if (!await context.Categories.AnyAsync())
        {
            var categories = new List<Category>
            {
                new() { Name = "All", Slug = "all", Description = "Explore the entire Shahi Zayka royal menu", DisplayOrder = 0 },
                new() { Name = "Biryani", Slug = "biryani", Description = "Aromatic basmati rice cooked with saffron and royal spices", DisplayOrder = 1 },
                new() { Name = "Curries", Slug = "curries", Description = "Rich, slow-simmered gravies prepared with authentic shahi recipe", DisplayOrder = 2 },
                new() { Name = "Rolls & Starters", Slug = "rolls", Description = "Crispy, hand-rolled kathi wraps and sizzling starters", DisplayOrder = 3 },
                new() { Name = "Royal Platters", Slug = "thali", Description = "Grand feasts fit for an emperor with accompaniments", DisplayOrder = 4 },
                new() { Name = "Vegetarian Specialties", Slug = "veg", Description = "Handcrafted pure vegetarian delicacies", DisplayOrder = 5 }
            };

            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        // 2. Seed Products if empty
        if (!await context.Products.AnyAsync())
        {
            var categories = await context.Categories.ToListAsync();
            var rollsCat = categories.First(c => c.Slug == "rolls");
            var curriesCat = categories.First(c => c.Slug == "curries");
            var biryaniCat = categories.First(c => c.Slug == "biryani");
            var thaliCat = categories.First(c => c.Slug == "thali");

            var products = new List<Product>
            {
                new()
                {
                    Name = "Chicken Roll",
                    Description = "Juicy charcoal-grilled chicken wrapped in a flaky, golden paratha with mint chutney and spiced onions.",
                    Price = 20.00m,
                    ImageUrl = "images/chicken-roll.png",
                    CategoryId = rollsCat.Id,
                    StockQuantity = 50,
                    IsAvailable = true,
                    IsFeatured = true,
                    IsVegetarian = false,
                    SpiceLevel = "Medium",
                    PreparationMinutes = 15,
                    AverageRating = 4.8,
                    ReviewCount = 84
                },
                new()
                {
                    Name = "Chicken Curry",
                    Description = "Tender bone-in chicken braised in a velvety onion, tomato, and Kashmiri chili gravy infused with whole spices.",
                    Price = 30.00m,
                    ImageUrl = "images/chicken curry.png",
                    CategoryId = curriesCat.Id,
                    StockQuantity = 40,
                    IsAvailable = true,
                    IsFeatured = true,
                    IsVegetarian = false,
                    SpiceLevel = "Hot",
                    PreparationMinutes = 25,
                    AverageRating = 4.9,
                    ReviewCount = 132
                },
                new()
                {
                    Name = "Fried Masala Chicken",
                    Description = "Crispy outside, succulent inside; marinated in royal curd, garlic, ginger, and crushed roasted spices.",
                    Price = 50.00m,
                    ImageUrl = "images/fried-masala-chicken.png",
                    CategoryId = rollsCat.Id,
                    StockQuantity = 35,
                    IsAvailable = true,
                    IsFeatured = true,
                    IsVegetarian = false,
                    SpiceLevel = "Hot",
                    PreparationMinutes = 20,
                    AverageRating = 4.7,
                    ReviewCount = 95
                },
                new()
                {
                    Name = "Lucknowi Chicken Biryani",
                    Description = "Fragrant long-grain basmati layered with marinated chicken, saffron milk, and fried shallots, slow-cooked in dum.",
                    Price = 30.00m,
                    ImageUrl = "images/lucknow chicken-biryani.png",
                    CategoryId = biryaniCat.Id,
                    StockQuantity = 60,
                    IsAvailable = true,
                    IsFeatured = true,
                    IsVegetarian = false,
                    SpiceLevel = "Medium",
                    PreparationMinutes = 30,
                    AverageRating = 5.0,
                    ReviewCount = 210
                },
                new()
                {
                    Name = "Punjabi Angithi",
                    Description = "A rustic platter featuring clay-oven tandoori chicken, seekh kebabs, butter naan, and dal makhani.",
                    Price = 90.00m,
                    ImageUrl = "images/punjabi angithi.png",
                    CategoryId = thaliCat.Id,
                    StockQuantity = 20,
                    IsAvailable = true,
                    IsFeatured = true,
                    IsVegetarian = false,
                    SpiceLevel = "Hot",
                    PreparationMinutes = 35,
                    AverageRating = 4.9,
                    ReviewCount = 68
                },
                new()
                {
                    Name = "Shahi Paneer",
                    Description = "Fresh cottage cheese cubes simmered in a regal gravy of cashew paste, cream, and green cardamom.",
                    Price = 40.00m,
                    ImageUrl = "images/shahi panner.png",
                    CategoryId = curriesCat.Id,
                    StockQuantity = 45,
                    IsAvailable = true,
                    IsFeatured = true,
                    IsVegetarian = true,
                    SpiceLevel = "Mild",
                    PreparationMinutes = 20,
                    AverageRating = 4.8,
                    ReviewCount = 140
                },
                new()
                {
                    Name = "Shahi Thali",
                    Description = "The ultimate royal vegetarian banquet: paneer butter masala, dal tadka, jeera rice, 2 rotis, raita, gulab jamun.",
                    Price = 100.00m,
                    ImageUrl = "images/shahi thali.png",
                    CategoryId = thaliCat.Id,
                    StockQuantity = 25,
                    IsAvailable = true,
                    IsFeatured = true,
                    IsVegetarian = true,
                    SpiceLevel = "Medium",
                    PreparationMinutes = 30,
                    AverageRating = 5.0,
                    ReviewCount = 180
                },
                new()
                {
                    Name = "Hyderabadi Veg Biryani",
                    Description = "Aromatic saffron basmati rice layered with garden vegetables, paneer, caramelized onions, and fresh mint.",
                    Price = 50.00m,
                    ImageUrl = "images/veg-biryani .png",
                    CategoryId = biryaniCat.Id,
                    StockQuantity = 40,
                    IsAvailable = true,
                    IsFeatured = true,
                    IsVegetarian = true,
                    SpiceLevel = "Medium",
                    PreparationMinutes = 25,
                    AverageRating = 4.7,
                    ReviewCount = 115
                }
            };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();
        }

        // 3. Seed Users if empty
        if (!await context.Users.AnyAsync())
        {
            var (adminHash, adminSalt) = passwordHasher.HashPassword("Admin@123");
            var (customerHash, customerSalt) = passwordHasher.HashPassword("Customer@123");

            var users = new List<User>
            {
                new()
                {
                    FullName = "Shahi Admin",
                    Email = "admin@shahizayka.com",
                    PasswordHash = adminHash,
                    Salt = adminSalt,
                    PhoneNumber = "+1 800-555-0199",
                    Address = "Royal Kitchen Central, Suite 101, New York, NY",
                    Role = UserRole.Admin,
                    IsActive = true
                },
                new()
                {
                    FullName = "Kaushik Customer",
                    Email = "customer@shahizayka.com",
                    PasswordHash = customerHash,
                    Salt = customerSalt,
                    PhoneNumber = "+1 800-555-0144",
                    Address = "42 Wallaby Way, Sydney / 123 Tech Blvd, Seattle, WA",
                    Role = UserRole.Customer,
                    IsActive = true
                }
            };

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();
        }

        // 4. Seed Reviews if empty
        if (!await context.Reviews.AnyAsync())
        {
            var reviews = new List<Review>
            {
                new()
                {
                    CustomerName = "Albert Leo",
                    Rating = 5,
                    Comment = "I've ordered from this place multiple times, and every single time, the experience has been outstanding. The food is always fresh, full of flavor, and clearly made with high-quality ingredients. What really sets them apart is their punctual delivery — my order always arrives hot and exactly when promised.",
                    ProfileImageUrl = "images/profile1.jpeg",
                    IsApproved = true
                },
                new()
                {
                    CustomerName = "Lisa Banker",
                    Rating = 5,
                    Comment = "The Shahi Thali and Lucknowi Biryani were phenomenal! The packaging kept everything piping hot, and the flavors transported us right to royal Lucknow. Highly recommended for family dinners.",
                    ProfileImageUrl = "images/profile2.jpeg",
                    IsApproved = true
                },
                new()
                {
                    CustomerName = "Elena Rostova",
                    Rating = 5,
                    Comment = "Best Indian food delivery in town. The Shahi Paneer is melt-in-the-mouth, and the customer service was extremely responsive when I made a custom delivery request.",
                    ProfileImageUrl = "images/profile3.jpeg",
                    IsApproved = true
                }
            };

            await context.Reviews.AddRangeAsync(reviews);
            await context.SaveChangesAsync();
        }
    }
}
