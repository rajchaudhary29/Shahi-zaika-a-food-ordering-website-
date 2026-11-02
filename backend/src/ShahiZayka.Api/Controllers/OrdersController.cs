using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShahiZayka.Application.DTOs;
using ShahiZayka.Application.Interfaces;

namespace ShahiZayka.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Places a new order with atomic stock validation, server-side pricing, and idempotency protection.
    /// </summary>
    [HttpPost("checkout")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Checkout(
        [FromBody] CheckoutRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKeyHeader)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKeyHeader))
        {
            request.IdempotencyKey = idempotencyKeyHeader;
        }

        int? userId = null;
        var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(subClaim, out var parsedId))
        {
            userId = parsedId;
        }

        var result = await _orderService.CreateOrderAsync(request, userId);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return StatusCode(result.StatusCode, result.Value);
    }

    /// <summary>
    /// Tracks an order by its unique order number (e.g. SZ-20261102-ABC123).
    /// </summary>
    [HttpGet("{orderNumber}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByOrderNumber(string orderNumber)
    {
        var order = await _orderService.GetOrderByOrderNumberAsync(orderNumber);
        if (order == null)
        {
            return NotFound(new { message = $"Order with number '{orderNumber}' was not found." });
        }
        return Ok(order);
    }

    /// <summary>
    /// Gets order history for the currently logged in user.
    /// </summary>
    [HttpGet("my-orders")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyOrders()
    {
        var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(subClaim, out var userId))
        {
            return Unauthorized();
        }

        var orders = await _orderService.GetOrdersByUserAsync(userId);
        return Ok(orders);
    }

    /// <summary>
    /// Gets all orders across the system (Admin only).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllOrders()
    {
        var orders = await _orderService.GetAllOrdersAsync();
        return Ok(orders);
    }

    /// <summary>
    /// Updates the fulfillment status of an order (Admin only).
    /// </summary>
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        var result = await _orderService.UpdateOrderStatusAsync(id, request.Status);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return Ok(result.Value);
    }
}
