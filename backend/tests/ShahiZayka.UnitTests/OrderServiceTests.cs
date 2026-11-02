using Microsoft.EntityFrameworkCore;
using Moq;
using ShahiZayka.Application.DTOs;
using ShahiZayka.Application.Services;
using ShahiZayka.Domain.Entities;
using ShahiZayka.Domain.Enums;
using ShahiZayka.Infrastructure.Data;
using Xunit;

namespace ShahiZayka.UnitTests;

public class OrderServiceTests : IDisposable
{
    private readonly ShahiZaykaDbContext _context;
    private readonly OrderService _orderService;

    public OrderServiceTests()
    {
        var options = new DbContextOptionsBuilder<ShahiZaykaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ShahiZaykaDbContext(options);
        _orderService = new OrderService(_context);

        // Seed test data
        var category = new Category { Id = 1, Name = "Biryani", Slug = "biryani" };
        _context.Categories.Add(category);

        var product1 = new Product
        {
            Id = 1,
            Name = "Lucknowi Chicken Biryani",
            Price = 30.00m,
            StockQuantity = 10,
            IsAvailable = true,
            CategoryId = 1
        };

        var product2 = new Product
        {
            Id = 2,
            Name = "Shahi Paneer",
            Price = 40.00m,
            StockQuantity = 0, // Out of stock
            IsAvailable = false,
            CategoryId = 1
        };

        _context.Products.AddRange(product1, product2);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task CreateOrder_WithValidCart_CalculatesCorrectPricingAndCreatesOrder()
    {
        // Arrange
        var request = new CheckoutRequest
        {
            CustomerName = "Kaushik",
            CustomerEmail = "kaushik@amazon.com",
            CustomerPhone = "1234567890",
            DeliveryAddress = "456 AWS Way, Seattle, WA",
            PaymentMethod = PaymentMethod.UPI,
            Items = new List<CartItemDto>
            {
                new() { ProductId = 1, Quantity = 2 } // 2 * $30 = $60
            }
        };

        // Act
        var result = await _orderService.CreateOrderAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(60.00m, result.Value.Subtotal);
        Assert.Equal(3.00m, result.Value.TaxAmount); // 5% GST on 60 = 3
        Assert.Equal(0.00m, result.Value.DeliveryFee); // Free over $50
        Assert.Equal(63.00m, result.Value.TotalAmount);
        Assert.Equal(OrderStatus.Confirmed, result.Value.Status);

        // Verify stock was decremented
        var product = await _context.Products.FindAsync(1);
        Assert.Equal(8, product!.StockQuantity); // 10 - 2 = 8
    }

    [Fact]
    public async Task CreateOrder_WithSameIdempotencyKey_ReturnsExistingOrderWithoutDuplicateCharge()
    {
        // Arrange
        var idempotencyKey = "UNIQUE-IDEMPOTENCY-KEY-12345";
        var request = new CheckoutRequest
        {
            IdempotencyKey = idempotencyKey,
            CustomerName = "Kaushik",
            CustomerEmail = "kaushik@amazon.com",
            CustomerPhone = "1234567890",
            DeliveryAddress = "456 AWS Way",
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1 } }
        };

        // Act 1: First request
        var firstResult = await _orderService.CreateOrderAsync(request);

        // Act 2: Network retry with identical idempotency key
        var secondResult = await _orderService.CreateOrderAsync(request);

        // Assert
        Assert.True(firstResult.IsSuccess);
        Assert.True(secondResult.IsSuccess);
        Assert.Equal(firstResult.Value!.OrderNumber, secondResult.Value!.OrderNumber);
        Assert.Equal(idempotencyKey, secondResult.Value.IdempotencyKey);

        // Verify stock was decremented ONLY ONCE
        var product = await _context.Products.FindAsync(1);
        Assert.Equal(9, product!.StockQuantity); // 10 - 1 = 9 (not 8!)
    }

    [Fact]
    public async Task CreateOrder_WhenProductOutOfStock_ReturnsFailure()
    {
        // Arrange
        var request = new CheckoutRequest
        {
            CustomerName = "Kaushik",
            CustomerEmail = "kaushik@amazon.com",
            CustomerPhone = "1234567890",
            DeliveryAddress = "456 AWS Way",
            Items = new List<CartItemDto> { new() { ProductId = 2, Quantity = 1 } } // Product 2 is out of stock
        };

        // Act
        var result = await _orderService.CreateOrderAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("out of stock", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateOrder_WithPromoCode_Applies10PercentDiscount()
    {
        // Arrange
        var request = new CheckoutRequest
        {
            CustomerName = "Kaushik",
            CustomerEmail = "kaushik@amazon.com",
            CustomerPhone = "1234567890",
            DeliveryAddress = "456 AWS Way",
            PromoCode = "ROYAL10",
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 1 } } // Subtotal = $30
        };

        // Act
        var result = await _orderService.CreateOrderAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(30.00m, result.Value!.Subtotal);
        Assert.Equal(3.00m, result.Value.DiscountAmount); // 10% of 30 = 3
        Assert.Equal(1.50m, result.Value.TaxAmount); // 5% of 30 = 1.50
        Assert.Equal(5.00m, result.Value.DeliveryFee); // Under 50, delivery fee is 5
        Assert.Equal(33.50m, result.Value.TotalAmount); // 30 + 1.50 + 5 - 3 = 33.50
    }

    [Fact]
    public async Task UpdateOrderStatus_ToCancelled_RestoresProductInventory()
    {
        // Arrange
        var request = new CheckoutRequest
        {
            CustomerName = "Kaushik",
            CustomerEmail = "kaushik@amazon.com",
            CustomerPhone = "1234567890",
            DeliveryAddress = "456 AWS Way",
            Items = new List<CartItemDto> { new() { ProductId = 1, Quantity = 3 } }
        };

        var created = await _orderService.CreateOrderAsync(request);
        Assert.True(created.IsSuccess);

        var productBeforeCancel = await _context.Products.FindAsync(1);
        Assert.Equal(7, productBeforeCancel!.StockQuantity); // 10 - 3 = 7

        // Act: Cancel order
        var cancelResult = await _orderService.UpdateOrderStatusAsync(created.Value!.Id, OrderStatus.Cancelled);

        // Assert
        Assert.True(cancelResult.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, cancelResult.Value!.Status);

        var productAfterCancel = await _context.Products.FindAsync(1);
        Assert.Equal(10, productAfterCancel!.StockQuantity); // Restored back to 10!
    }
}
