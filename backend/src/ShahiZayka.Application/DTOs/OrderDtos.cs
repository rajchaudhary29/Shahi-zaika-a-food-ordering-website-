using ShahiZayka.Domain.Enums;

namespace ShahiZayka.Application.DTOs;

public class CartItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class CheckoutRequest
{
    public string? IdempotencyKey { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string? SpecialInstructions { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;
    public string? PromoCode { get; set; }
    public List<CartItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string? SpecialInstructions { get; set; }
    
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public PaymentMethod PaymentMethod { get; set; }
    public string PaymentMethodName => PaymentMethod.ToString();
    public PaymentStatus PaymentStatus { get; set; }
    public string PaymentStatusName => PaymentStatus.ToString();

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? EstimatedDeliveryUtc { get; set; }

    public List<OrderItemDto> Items { get; set; } = new();
}

public class UpdateOrderStatusRequest
{
    public OrderStatus Status { get; set; }
}
