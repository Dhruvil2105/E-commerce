using ECommerce.Inventory.Services;
using ECommerce.Shared.Events;
using MassTransit;

namespace ECommerce.Inventory.Consumers
{
    public class StockReleasedConsumer : IConsumer<StockReleasedEvent>
    {
        private readonly IInventoryService _inventoryService;
        private readonly ILogger<StockReleasedConsumer> _logger;

        public StockReleasedConsumer(IInventoryService inventoryService, ILogger<StockReleasedConsumer> logger)
        {
            _logger = logger;
            _inventoryService = inventoryService;
        }
        public async Task Consume(ConsumeContext<StockReleasedEvent> context)
        {
            var message = context.Message;

            _logger.LogInformation(
           "StockReleasedEvent received: OrderId={OrderId}",
           message.OrderId);

            await _inventoryService
                .ReleaseStockAsync(message.OrderId, message.TenantId);
        }
    }
}
