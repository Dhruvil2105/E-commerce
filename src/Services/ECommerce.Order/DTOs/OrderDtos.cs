using ECommerce.Order.Models;

namespace ECommerce.Order.DTOs
{
    /// <summary>
    /// What the customer sends when placing an order.
    /// </summary>
    public record PlaceOrderRequest
    {
        /// <summary>List of items the customer wants to buy.</summary>
        public List<OrderItemRequest> Items { get; init; } = [];
    }

    public record OrderItemRequest
    {
        public Guid ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public decimal UnitPrice { get; init; }
    }

    /// <summary>
    /// What we return to the customer.
    /// </summary>
    public record OrderDto
    {
        public Guid Id { get; init; }
        public string UserId { get; init; } = string.Empty;
        public OrderStatus Status { get; init; }
        public string StatusName { get; init; } = string.Empty;
        public decimal Total { get; init; }
        public string? CancellationReason { get; init; }
        public string? TrackingNumber { get; init; }
        public string? Courier { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public List<OrderItemDto> Items { get; init; } = [];
    }

    public record OrderItemDto
    {
        public Guid ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public decimal UnitPrice { get; init; }
        public decimal Subtotal { get; init; }
    }
}
