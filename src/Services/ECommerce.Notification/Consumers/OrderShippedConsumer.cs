using ECommerce.Notification.Services;
using ECommerce.Shared.Events;
using MassTransit;

namespace ECommerce.Notification.Consumers;

/// <summary>
/// Sends shipping notification when order is shipped.
///
/// TRIGGERED BY:
/// Order Service publishes OrderShippedEvent when
/// the warehouse ships the order.
/// </summary>
public class OrderShippedConsumer : IConsumer<OrderShippedEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<OrderShippedConsumer> _logger;

    public OrderShippedConsumer(
        IEmailService emailService,
        ILogger<OrderShippedConsumer> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderShippedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "OrderShippedEvent received: OrderId={OrderId} " +
            "Tracking={TrackingNumber} Courier={Courier}",
            message.OrderId, message.TrackingNumber, message.Courier);

        var userEmail = $"user_{message.UserId}@example.com";
        var fullName = "Valued Customer";

        await _emailService.SendOrderShippedAsync(
            userEmail,
            fullName,
            message.OrderId,
            message.TrackingNumber,
            message.Courier);
    }
}