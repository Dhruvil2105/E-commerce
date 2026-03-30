using ECommerce.Inventory.Services;
using ECommerce.Shared.Events;
using MassTransit;

namespace ECommerce.Inventory.Consumers;

/// <summary>
/// Consumes ProductCreatedEvent published by Product Service.
///
/// WHAT THIS DOES:
/// When admin creates a new product in Product Service,
/// this consumer automatically creates a stock record
/// in Inventory Service with InitialStock = 0.
///
/// Admin then uses PUT /api/inventory/{productId}
/// to set the actual stock quantity.
///
/// WHY THIS PATTERN:
/// Product Service and Inventory Service are decoupled.
/// Product Service never calls Inventory Service directly.
/// It just publishes an event and moves on.
/// Inventory Service reacts independently.
/// </summary>
public class ProductCreatedConsumer : IConsumer<ProductCreatedEvent>
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<ProductCreatedConsumer> _logger;

    public ProductCreatedConsumer(
        IInventoryService inventoryService,
        ILogger<ProductCreatedConsumer> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "ProductCreatedEvent received: ProductId={ProductId} " +
            "Name={Name} InitialStock={Stock}",
            message.ProductId, message.Name, message.InitialStock);

        // Create stock record in Inventory database
        await _inventoryService.CreateStockItemAsync(
            message.ProductId,
            message.Name,
            message.InitialStock,
            message.TenantId);
    }
}