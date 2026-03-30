using ECommerce.Notification.Services;
using ECommerce.Shared.Events;
using MassTransit;

namespace ECommerce.Notification.Consumers;

/// <summary>
/// Sends cancellation email when an order is cancelled.
///
/// TRIGGERED BY:
/// Order Service publishes OrderCancelledEvent when:
///   - Payment fails
///   - Stock reservation fails
///   - Customer cancels the order
/// </summary>
public class OrderCancelledConsumer : IConsumer<OrderCancelledEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<OrderCancelledConsumer> _logger;

    public OrderCancelledConsumer(
        IEmailService emailService,
        ILogger<OrderCancelledConsumer> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCancelledEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "OrderCancelledEvent received: OrderId={OrderId} " +
            "Reason={Reason}",
            message.OrderId, message.Reason);

        var userEmail = $"user_{message.UserId}@example.com";
        var fullName = "Valued Customer";

        await _emailService.SendOrderCancelledAsync(
            userEmail,
            fullName,
            message.OrderId,
            message.Reason ?? "No reason provided");
    }
}