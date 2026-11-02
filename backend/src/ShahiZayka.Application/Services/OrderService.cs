using Microsoft.EntityFrameworkCore;
using ShahiZayka.Application.Common;
using ShahiZayka.Application.DTOs;
using ShahiZayka.Application.Interfaces;
using ShahiZayka.Domain.Entities;
using ShahiZayka.Domain.Enums;

namespace ShahiZayka.Application.Services;

public class OrderService : IOrderService
{
    private readonly IApplicationDbContext _context;

    public OrderService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<OrderDto>> CreateOrderAsync(CheckoutRequest request, int? userId = null)
    {
        // 1. Idempotency Check (Critical for Amazon SDE / Distributed Systems)
        var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? Guid.NewGuid().ToString()
            : request.IdempotencyKey.Trim();

        var existingOrder = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey);

        if (existingOrder != null)
        {
            // Idempotent replay: return existing order to client without re-charging or re-creating
            return Result<OrderDto>.Success(MapToDto(existingOrder));
        }

        // 2. Validate Cart Items
        if (request.Items == null || !request.Items.Any())
        {
            return Result<OrderDto>.Failure("Your cart is empty. Please add items before checking out.", 400);
        }

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var orderItems = new List<OrderItem>();
        decimal subtotal = 0;

        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
            {
                return Result<OrderDto>.Failure($"Invalid quantity {item.Quantity} for product ID {item.ProductId}.", 400);
            }

            if (!products.TryGetValue(item.ProductId, out var product))
            {
                return Result<OrderDto>.Failure($"Product with ID {item.ProductId} no longer exists.", 400);
            }

            if (!product.IsAvailable)
            {
                return Result<OrderDto>.Failure($"'{product.Name}' is currently out of stock.", 400);
            }

            if (product.StockQuantity < item.Quantity)
            {
                return Result<OrderDto>.Failure($"Only {product.StockQuantity} unit(s) of '{product.Name}' available.", 400);
            }

            // Reserve stock (Optimistic concurrency pattern)
            product.StockQuantity -= item.Quantity;

            var itemTotal = product.Price * item.Quantity;
            subtotal += itemTotal;

            orderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = item.Quantity,
                TotalPrice = itemTotal,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        // 3. Financial calculations
        decimal taxAmount = Math.Round(subtotal * 0.05m, 2); // 5% GST
        decimal deliveryFee = subtotal >= 50m ? 0m : 5.00m; // Free delivery over $50
        decimal discountAmount = 0m;

        if (!string.IsNullOrWhiteSpace(request.PromoCode))
        {
            var code = request.PromoCode.Trim().ToUpperInvariant();
            if (code == "ROYAL10")
            {
                discountAmount = Math.Round(subtotal * 0.10m, 2); // 10% off
            }
            else if (code == "SHAHI5" || code == "WELCOME")
            {
                discountAmount = Math.Min(subtotal, 5.00m); // $5 off
            }
        }

        decimal totalAmount = Math.Max(0m, subtotal + taxAmount + deliveryFee - discountAmount);

        // 4. Create Order Entity
        var orderNumber = $"SZ-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        var order = new Order
        {
            OrderNumber = orderNumber,
            IdempotencyKey = idempotencyKey,
            UserId = userId,
            CustomerName = request.CustomerName.Trim(),
            CustomerEmail = request.CustomerEmail.Trim(),
            CustomerPhone = request.CustomerPhone.Trim(),
            DeliveryAddress = request.DeliveryAddress.Trim(),
            SpecialInstructions = request.SpecialInstructions?.Trim(),
            Subtotal = subtotal,
            TaxAmount = taxAmount,
            DeliveryFee = deliveryFee,
            DiscountAmount = discountAmount,
            TotalAmount = totalAmount,
            Status = OrderStatus.Confirmed,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = request.PaymentMethod == PaymentMethod.CashOnDelivery ? PaymentStatus.Pending : PaymentStatus.Paid,
            PaymentTransactionId = request.PaymentMethod != PaymentMethod.CashOnDelivery ? $"TXN-{Guid.NewGuid().ToString("N")[..10].ToUpper()}" : null,
            CreatedAtUtc = DateTime.UtcNow,
            EstimatedDeliveryUtc = DateTime.UtcNow.AddMinutes(35),
            Items = orderItems
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return Result<OrderDto>.Success(MapToDto(order), 201);
    }

    public async Task<OrderDto?> GetOrderByOrderNumberAsync(string orderNumber)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber.Trim());

        return order == null ? null : MapToDto(order);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);

        return order == null ? null : MapToDto(order);
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersByUserAsync(int userId)
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync();

        return orders.Select(MapToDto);
    }

    public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync()
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAtUtc)
            .AsNoTracking()
            .ToListAsync();

        return orders.Select(MapToDto);
    }

    public async Task<Result<OrderDto>> UpdateOrderStatusAsync(int id, OrderStatus status)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return Result<OrderDto>.Failure($"Order with ID {id} not found.", 404);
        }

        // Validate state transitions
        if (order.Status == OrderStatus.Delivered && status != OrderStatus.Delivered)
        {
            return Result<OrderDto>.Failure("Cannot change status of an already delivered order.", 400);
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return Result<OrderDto>.Failure("Cannot change status of a cancelled order.", 400);
        }

        // Handle cancellation inventory roll-back
        if (status == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled)
        {
            var productIds = order.Items.Select(i => i.ProductId).ToList();
            var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();
            foreach (var item in order.Items)
            {
                var prod = products.FirstOrDefault(p => p.Id == item.ProductId);
                if (prod != null)
                {
                    prod.StockQuantity += item.Quantity;
                }
            }
        }

        order.Status = status;
        order.UpdatedAtUtc = DateTime.UtcNow;

        if (status == OrderStatus.Delivered && order.PaymentMethod == PaymentMethod.CashOnDelivery)
        {
            order.PaymentStatus = PaymentStatus.Paid;
        }

        await _context.SaveChangesAsync();

        return Result<OrderDto>.Success(MapToDto(order));
    }

    private static OrderDto MapToDto(Order o) => new()
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        IdempotencyKey = o.IdempotencyKey,
        CustomerName = o.CustomerName,
        CustomerEmail = o.CustomerEmail,
        CustomerPhone = o.CustomerPhone,
        DeliveryAddress = o.DeliveryAddress,
        SpecialInstructions = o.SpecialInstructions,
        Subtotal = o.Subtotal,
        TaxAmount = o.TaxAmount,
        DeliveryFee = o.DeliveryFee,
        DiscountAmount = o.DiscountAmount,
        TotalAmount = o.TotalAmount,
        Status = o.Status,
        PaymentMethod = o.PaymentMethod,
        PaymentStatus = o.PaymentStatus,
        CreatedAtUtc = o.CreatedAtUtc,
        EstimatedDeliveryUtc = o.EstimatedDeliveryUtc,
        Items = o.Items.Select(i => new OrderItemDto
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity,
            TotalPrice = i.TotalPrice
        }).ToList()
    };
}
