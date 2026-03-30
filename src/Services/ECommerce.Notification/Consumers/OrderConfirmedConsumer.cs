using ECommerce.Notification.Services;
using ECommerce.Shared.Events;
using MassTransit;

namespace ECommerce.Notification.Consumers;

/// <summary>
/// Sends confirmation email when an order is confirmed.
///
/// TRIGGERED BY:
/// Order Service publishes OrderConfirmedEvent after
/// both payment and stock reservation succeed.
/// </summary>
public class OrderConfirmedConsumer : IConsumer<OrderConfirmedEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<OrderConfirmedConsumer> _logger;

    public OrderConfirmedConsumer(
        IEmailService emailService,
        ILogger<OrderConfirmedConsumer> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderConfirmedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "OrderConfirmedEvent received: OrderId={OrderId} " +
            "UserId={UserId}",
            message.OrderId, message.UserId);

        // In a real system we would fetch user email from
        // Identity Service or embed it in the event.
        // For now we use a placeholder.
        // TODO: Embed UserEmail in OrderConfirmedEvent
        var userEmail = $"user_{message.UserId}@example.com";
        var fullName = "Valued Customer";

        await _emailService.SendOrderConfirmedAsync(
            userEmail,
            fullName,
            message.OrderId,
            message.Total);
    }
}