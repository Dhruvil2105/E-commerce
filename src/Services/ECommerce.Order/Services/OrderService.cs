using ECommerce.Order.Data;
using ECommerce.Order.DTOs;
using ECommerce.Order.Interface;
using ECommerce.Order.Models;
using ECommerce.Shared.DTOs;
using ECommerce.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Order.Services
{
    public class OrderService : IOrderService
    {
        private readonly OrderDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<OrderService> _logger;

        public OrderService(OrderDbContext context, IPublishEndpoint publishEndpoint, ILogger<OrderService> logger)
        {
            _context = context;
            _logger = logger;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<ApiResponse<OrderDto>> GetByIdAsync(Guid id, string userId, string tenantId)
        {
            var order = await _context.Orders
                                .Include(o => o.Items)
                                .FirstOrDefaultAsync(o =>
                                    o.Id == id &&
                                    o.UserId == userId &&
                                    o.TenantId == tenantId);

            if (order is null)
                return ApiResponse<OrderDto>.Fail("Order not found");

            return ApiResponse<OrderDto>.Ok(MapToDto(order));
        }

        public async Task<ApiResponse<List<OrderDto>>> GetMyOrdersAsync(string userId, string tenantId)
        {
            var orders = await _context.Orders
                                 .Include(o => o.Items)
                                 .Where(o => o.UserId == userId && o.TenantId == tenantId)
                                 .OrderByDescending(o => o.CreatedAt)
                                 .ToListAsync();

            return ApiResponse<List<OrderDto>>.Ok(orders.Select(MapToDto).ToList());
        }

        /// <summary>
        /// Places a new order and kicks off the Saga.
        ///
        /// FLOW:
        ///   1. Validate request
        ///   2. Create order with status PENDING
        ///   3. Save to database
        ///   4. Publish OrderPlacedEvent to RabbitMQ
        ///   5. Return immediately — don't wait for Saga to complete
        ///
        /// The Saga continues asynchronously:
        ///   Payment Service  receives OrderPlacedEvent → charges customer
        ///   Inventory Service receives OrderPlacedEvent → reserves stock
        ///   Order Service    receives results → confirms or cancels
        /// </summary>
        /// 
        public async Task<ApiResponse<OrderDto>> PlaceOrderAsync(PlaceOrderRequest request, string userId, string tenantId)
        {
            // Validate request
            if (request.Items == null || request.Items.Count == 0)
                return ApiResponse<OrderDto>.Fail("Order must have at least one item");


            foreach (var item in request.Items)
            {
                if (item.Quantity <= 0)
                    return ApiResponse<OrderDto>.Fail(
                        $"Invalid quantity for product {item.ProductId}");

                if (item.UnitPrice <= 0)
                    return ApiResponse<OrderDto>.Fail(
                        $"Invalid price for product {item.ProductId}");
            }

            // Calculate total
            var total = request.Items
                .Sum(i => i.Quantity * i.UnitPrice);

            // Create order entity
            var order = new Models.Order
            {
                UserId = userId,
                TenantId = tenantId,
                Status = OrderStatus.Pending,
                Total = total,
                Items = request.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Subtotal = i.Quantity * i.UnitPrice,
                }).ToList(),
            };

            // Save to database
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Order created: {OrderId} for user {UserId} " + "total {Total} items {ItemCount}", order.Id, userId, total, order.Items.Count);

            await _publishEndpoint.Publish(new OrderPlacedEvent
            {
                TraceId = Guid.NewGuid().ToString(),
                TenantId = tenantId,

                OrderId = order.Id,
                UserId = userId,
                Total = total,
                Items = order.Items.Select(i => new Shared.Events.OrderItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList(),
            });

            _logger.LogInformation("OrderPlacedEvent published for order {OrderId}", order.Id);

            return ApiResponse<OrderDto>.Ok(MapToDto(order));
        }

        private static OrderDto MapToDto(Models.Order o) => new()
        {
            Id = o.Id,
            UserId = o.UserId,
            Status = o.Status,
            StatusName = o.Status.ToString(),
            Total = o.Total,
            CancellationReason = o.CancellationReason,
            TrackingNumber = o.TrackingNumber,
            Courier = o.Courier,
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt,
            Items = o.Items.Select(i => new DTOs.OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Subtotal = i.Subtotal,
            }).ToList(),
        };
    }
}
