using ECommerce.Order.Data;
using ECommerce.Order.Models;
using ECommerce.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Order.Consumers
{
    /// <summary>
    /// Consumes PaymentProcessedEvent published by Payment Service.
    ///
    /// WHAT THIS DOES:
    /// When Payment Service finishes processing payment it publishes
    /// PaymentProcessedEvent with Success = true or false.
    /// This consumer receives that event and updates the order.
    ///
    /// SAGA STEP:
    /// If both PaymentProcessed AND StockReserved are true:
    ///   → both results received → decide to confirm or cancel
    ///
    /// IDEMPOTENCY:
    /// This consumer checks if it already processed this event
    /// to handle duplicate message delivery from RabbitMQ.
    /// </summary>
    public class PaymentResultConsumer : IConsumer<PaymentProcessedEvent>
    {
        private readonly OrderDbContext _context;
        private readonly ILogger<PaymentResultConsumer> _logger;
        private readonly IPublishEndpoint _publishEndpoint;

        public PaymentResultConsumer(OrderDbContext context, ILogger<PaymentResultConsumer> logger, IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _logger = logger;
            _publishEndpoint = publishEndpoint;
        }

        public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
        {
            var message = context.Message;

            _logger.LogInformation("PaymentProcessedEvent received: OrderId={OrderId} " + "Success={Success}", message.OrderId, message.Success);

            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == message.OrderId);

            if (order is null)
            {
                _logger.LogWarning("Order not found for payment result: {OrderId}", message.OrderId);
                return;
            }

            if (order.PaymentProcessed)
            {
                _logger.LogWarning("Duplicate PaymentProcessedEvent for order {OrderId} - skipping", message.OrderId);
                return;
            }

            order.PaymentProcessed = true;
            order.PaymentSuccess = message.Success;
            order.UpdatedAt = DateTime.UtcNow;

            if (!message.Success)
            {
                order.Status = Models.OrderStatus.Cancelled;
                order.CancellationReason = message.FailureReason ?? "Payment was decliened";

                await _context.SaveChangesAsync();

                await _publishEndpoint.Publish(new OrderCancelledEvent
                {
                    OrderId = order.Id,
                    UserId = order.UserId,
                    TenantId = order.TenantId,
                    Reason = order.CancellationReason,
                });

                if (order.StockReserved && order.StockSuccess)
                {
                    await _publishEndpoint.Publish(new StockReleasedEvent
                    {
                        OrderId = order.Id,
                        TenantId = order.TenantId
                    });
                }

                _logger.LogInformation("Order {OrderId} cancelled due to payment failure:{Reson}", order.Id, order.CancellationReason);

                return;
            }
            // Payment succeeded — check if stock result also arrived
            await TryCompleteOrderAsync(order);
            await _context.SaveChangesAsync();

        }

        /// <summary>
        /// Checks if both payment AND stock results have arrived.
        /// If yes — confirms or cancels the order.
        /// </summary>


        private async Task TryCompleteOrderAsync(Models.Order order)
        {
            // Both results must be in before we can decide
            if (!order.PaymentProcessed || !order.StockReserved)
            {
                _logger.LogInformation("Order {OrderId} waiting for more results. " + "Payment = {Payment} Stock = {Stock}", order.Id, order.PaymentProcessed, order.StockReserved);
                return;
            }

            if (order.PaymentSuccess && order.StockSuccess)
            {
                order.Status = OrderStatus.Confirmed;
                order.UpdatedAt = DateTime.UtcNow;

                await _publishEndpoint.Publish(new OrderConfirmedEvent
                {
                    OrderId = order.Id,
                    UserId = order.UserId,
                    TenantId = order.TenantId,
                    Total = order.Total,
                });

                _logger.LogInformation("Order {OrderId} CONFIRMED - payment and stock both succeeded", order.Id);
            }
            else
            {
                order.Status = OrderStatus.Cancelled;
                order.CancellationReason = !order.StockSuccess ? "Insufficient stock" : "Payment failed";

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
