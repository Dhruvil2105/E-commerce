using ECommerce.Inventory.Services;
using ECommerce.Shared.Events;
using MassTransit;

namespace ECommerce.Inventory.Consumers
{
    public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
    {
        private readonly IInventoryService _inventoryService;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<OrderPlacedConsumer> _logger;

        public OrderPlacedConsumer(IInventoryService inventory, IPublishEndpoint publishEndpoint, ILogger<OrderPlacedConsumer> logger)
        {
            _inventoryService = inventory;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
        {
            var message = context.Message;

            _logger.LogInformation("OrderPlacedEvent received by Inventory: " + "OrderId={OrderId} Items = {ItemCount}", message.OrderId, message.Items.Count);

            // Build list of items to reserve
            var itemsToReserve = message.Items
                .Select(i => (i.ProductId, i.Quantity))
                .ToList();


            // Try to reserve stock
            var result = await _inventoryService.ReserveStockAsync(message.OrderId, itemsToReserve, message.TenantId);

            // Publish result back to Order Service
            await _publishEndpoint.Publish(new StockReservedEvent
            {
                TraceId = message.TraceId,
                TenantId = message.TenantId,
                OrderId = message.OrderId,
                Success = result.Success,
                FailureReason = result.Success ? null : result.Error,
            });

            _logger.LogInformation(
            "StockReservedEvent published: OrderId={OrderId} " +
            "Success={Success}",
            message.OrderId, result.Success);
        }
    }
}
