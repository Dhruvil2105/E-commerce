using System.Collections.Immutable;
using ECommerce.Order.Data;
using ECommerce.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Order.Consumers
{
    /// <summary>
    /// Consumes StockReservedEvent published by Inventory Service.
    ///
    /// Mirror of PaymentResultConsumer — same logic but for stock.
    /// </summary>
    public class StockResultConsumer : IConsumer<StockReservedEvent>
    {
        private readonly OrderDbContext _context;
        private readonly ILogger<StockResultConsumer> _logger;
        private readonly IPublishEndpoint _publishEndpoint;

        public StockResultConsumer(OrderDbContext context, ILogger<StockResultConsumer> logger, IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _logger = logger;
            _publishEndpoint = publishEndpoint;
        }

        public async Task Consume(ConsumeContext<StockReservedEvent> context)
        {
            var message = context.Message;

            _logger.LogInformation("StockReservedEvent received: OrderId = {OrderId}" + "success = {Success}", message.OrderId, message.Success);

            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == message.OrderId);

            if (order is null)
            {
                _logger.LogWarning("Order not found for stock result:{orderId}", message.OrderId);
                return;
            }

            if (order.StockReserved)
            {
                _logger.LogWarning("Duplicate StockReservedEvent for order {OrderId} - skipping", message.OrderId);

                return;
            }

            order.StockReserved = true;
            order.StockSuccess = message.Success;
            order.UpdatedAt = DateTime.UtcNow;

            if (!message.Success)
            {
                order.Status = Models.OrderStatus.Cancelled;
                order.CancellationReason = message.FailureReason ?? "Insufficient stock";

                await _context.SaveChangesAsync();

                await _publishEndpoint.Publish(new OrderCancelledEvent
                {
                    OrderId = order.Id,
                    UserId = order.UserId,
                    TenantId = order.TenantId,
                    Reason = order.CancellationReason
                });

                // If payment was already charged → release it
                if (order.PaymentProcessed && order.PaymentSuccess)
                {
                    // In a real system publish PaymentRefundEvent
                    // For now we log it
                    _logger.LogInformation(
                        "Payment refund needed for order {OrderId}",
                        order.Id);
                }

                _logger.LogInformation("Order {OrderId} cancelled due to stock failure:{Reason}", order.Id, order.CancellationReason);

                return;
            }

            // Stock succeeded — check if payment result also arrived
            await TryCompleteOrderAsync(order);
            await _context.SaveChangesAsync();
        }

        private async Task TryCompleteOrderAsync(Models.Order order)
        {
            if (!order.PaymentProcessed || !order.StockReserved)
            {
                _logger.LogInformation("Order {OrderId} waiting for more results. " + "Payment={Payment} Stock={Stock}", order.Id, order.PaymentProcessed, order.StockReserved);
                return;
            }

            if(order.PaymentSuccess && order.StockSuccess)
            {
                order.Status = Models.OrderStatus.Confirmed;
                order.UpdatedAt = DateTime.UtcNow;

                await _publishEndpoint.Publish(new OrderConfirmedEvent
                {
                    OrderId =order.Id,
                    UserId =order.UserId,
                    TenantId = order.TenantId,
                    Total = order.Total,
                });

                _logger.LogInformation("Order {OrderId} CONFIRMED", order.Id);
            }
            else
            {
                order.Status = Models.OrderStatus.Cancelled;
                order.CancellationReason = !order.PaymentSuccess ? "Payment failed" : "Insufficient stock";

                await _publishEndpoint.Publish(new OrderCancelledEvent
                {
                    OrderId = order.Id,
                    UserId = order.UserId,
                    TenantId = order.TenantId,
                    Reason = order.CancellationReason,
                });

                _logger.LogInformation("Order {OrderId} CANCELLED: {Reason}", order.Id, order.CancellationReason);
            }
        }
    }
}
