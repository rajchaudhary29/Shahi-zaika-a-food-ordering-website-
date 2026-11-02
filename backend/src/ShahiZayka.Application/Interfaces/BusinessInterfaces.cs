using ShahiZayka.Application.Common;
using ShahiZayka.Application.DTOs;
using ShahiZayka.Domain.Enums;

namespace ShahiZayka.Application.Interfaces;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllProductsAsync(int? categoryId = null, string? search = null, bool? isVegetarian = null);
    Task<ProductDto?> GetProductByIdAsync(int id);
    Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
    Task<ProductDto> CreateProductAsync(CreateProductRequest request);
    Task<ProductDto?> UpdateProductAsync(int id, UpdateProductRequest request);
    Task<bool> DeleteProductAsync(int id);
}

public interface IOrderService
{
    Task<Result<OrderDto>> CreateOrderAsync(CheckoutRequest request, int? userId = null);
    Task<OrderDto?> GetOrderByOrderNumberAsync(string orderNumber);
    Task<OrderDto?> GetOrderByIdAsync(int id);
    Task<IEnumerable<OrderDto>> GetOrdersByUserAsync(int userId);
    Task<IEnumerable<OrderDto>> GetAllOrdersAsync();
    Task<Result<OrderDto>> UpdateOrderStatusAsync(int id, OrderStatus status);
}

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request);
    Task<UserDto?> GetProfileAsync(int userId);
}

public interface IReviewService
{
    Task<IEnumerable<ReviewDto>> GetReviewsAsync();
    Task<ReviewDto> AddReviewAsync(CreateReviewRequest request);
}

public interface INewsletterService
{
    Task<Result<bool>> SubscribeAsync(string email);
}
