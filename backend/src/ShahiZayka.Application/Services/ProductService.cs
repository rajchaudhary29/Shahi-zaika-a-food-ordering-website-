using Microsoft.EntityFrameworkCore;
using ShahiZayka.Application.DTOs;
using ShahiZayka.Application.Interfaces;
using ShahiZayka.Domain.Entities;
using ShahiZayka.Domain.Exceptions;

namespace ShahiZayka.Application.Services;

public class ProductService : IProductService
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cacheService;
    private const string ProductsCacheKey = "products_all";
    private const string CategoriesCacheKey = "categories_all";

    public ProductService(IApplicationDbContext context, ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync(int? categoryId = null, string? search = null, bool? isVegetarian = null)
    {
        // Cache-aside pattern for full catalog query
        var products = await _cacheService.GetAsync<List<ProductDto>>(ProductsCacheKey);

        if (products == null)
        {
            products = await _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : "General",
                    StockQuantity = p.StockQuantity,
                    IsAvailable = p.IsAvailable,
                    IsFeatured = p.IsFeatured,
                    IsVegetarian = p.IsVegetarian,
                    SpiceLevel = p.SpiceLevel,
                    PreparationMinutes = p.PreparationMinutes,
                    AverageRating = p.AverageRating,
                    ReviewCount = p.ReviewCount
                })
                .ToListAsync();

            // Cache for 10 minutes
            await _cacheService.SetAsync(ProductsCacheKey, products, TimeSpan.FromMinutes(10));
        }

        var query = products.AsQueryable();

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLowerInvariant();
            query = query.Where(p => p.Name.ToLowerInvariant().Contains(searchLower) ||
                                     p.Description.ToLowerInvariant().Contains(searchLower) ||
                                     p.CategoryName.ToLowerInvariant().Contains(searchLower));
        }

        if (isVegetarian.HasValue)
        {
            query = query.Where(p => p.IsVegetarian == isVegetarian.Value);
        }

        return query.ToList();
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return null;

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            ImageUrl = product.ImageUrl,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? "General",
            StockQuantity = product.StockQuantity,
            IsAvailable = product.IsAvailable,
            IsFeatured = product.IsFeatured,
            IsVegetarian = product.IsVegetarian,
            SpiceLevel = product.SpiceLevel,
            PreparationMinutes = product.PreparationMinutes,
            AverageRating = product.AverageRating,
            ReviewCount = product.ReviewCount
        };
    }

    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
    {
        var cached = await _cacheService.GetAsync<List<CategoryDto>>(CategoriesCacheKey);
        if (cached != null) return cached;

        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                DisplayOrder = c.DisplayOrder,
                ProductCount = c.Products.Count
            })
            .ToListAsync();

        await _cacheService.SetAsync(CategoriesCacheKey, categories, TimeSpan.FromMinutes(30));
        return categories;
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            ImageUrl = request.ImageUrl,
            CategoryId = request.CategoryId,
            StockQuantity = request.StockQuantity,
            IsAvailable = request.IsAvailable,
            IsFeatured = request.IsFeatured,
            IsVegetarian = request.IsVegetarian,
            SpiceLevel = request.SpiceLevel,
            PreparationMinutes = request.PreparationMinutes,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Invalidate cache
        await _cacheService.RemoveAsync(ProductsCacheKey);
        await _cacheService.RemoveAsync(CategoriesCacheKey);

        return (await GetProductByIdAsync(product.Id))!;
    }

    public async Task<ProductDto?> UpdateProductAsync(int id, UpdateProductRequest request)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return null;

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.ImageUrl = request.ImageUrl;
        product.CategoryId = request.CategoryId;
        product.StockQuantity = request.StockQuantity;
        product.IsAvailable = request.IsAvailable;
        product.IsFeatured = request.IsFeatured;
        product.IsVegetarian = request.IsVegetarian;
        product.SpiceLevel = request.SpiceLevel;
        product.PreparationMinutes = request.PreparationMinutes;
        product.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Invalidate cache
        await _cacheService.RemoveAsync(ProductsCacheKey);
        await _cacheService.RemoveAsync(CategoriesCacheKey);

        return await GetProductByIdAsync(id);
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        await _cacheService.RemoveAsync(ProductsCacheKey);
        await _cacheService.RemoveAsync(CategoriesCacheKey);

        return true;
    }
}
