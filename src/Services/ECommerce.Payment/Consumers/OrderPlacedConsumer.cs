using ECommerce.Payment.Services;
using ECommerce.Shared.Events;
using MassTransit;

namespace ECommerce.Payment.Consumers
{
    /// <summary>
    /// Consumes OrderPlacedEvent published by Order Service.
    ///
    /// WHAT THIS DOES:
    ///   1. Receives OrderPlacedEvent from RabbitMQ
    ///   2. Calls PaymentService to charge the customer
    ///   3. Publishes PaymentProcessedEvent back to RabbitMQ
    ///   4. Order Service receives PaymentProcessedEvent
    ///
    /// This is Step 2 of the Saga.
    /// </summary>
    public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
    {
        private readonly IPaymentService _paymentService;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<OrderPlacedConsumer> _logger;

        public OrderPlacedConsumer(IPaymentService paymentService, IPublishEndpoint publishEndpoint, ILogger<OrderPlacedConsumer> logger)
        {
            _paymentService = paymentService;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
        {
            var message = context.Message;

            _logger.LogInformation("OrderPlacedEvent received: OrderId = {OrderId} " + "Total ={Total} UserId={UserId}", message.OrderId, message.Total, message.UserId);

            var result = await _paymentService.ProcessPaymentAsync(message.OrderId, message.UserId, message.TenantId, message.Total);

            await _publishEndpoint.Publish(new PaymentProcessedEvent
            {
                TraceId = message.TraceId,
                TenantId = message.TenantId,
                OrderId = message.OrderId,
                Amount = message.Total,
                Success = result.Data?.Success ?? false,
                ChargeId = result.Data?.ChargeId ?? string.Empty,
                FailureReason = result.Data?.FailurReason,

            });

            _logger.LogInformation(
            "PaymentProcessedEvent published: OrderId={OrderId} " +
            "Success={Success}",
            message.OrderId, result.Data?.Success);
        }
    }
}
