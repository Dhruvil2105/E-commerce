using ECommerce.Inventory.Data;
using ECommerce.Inventory.DTOs;
using ECommerce.Inventory.Models;
using ECommerce.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Inventory.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly InventoryDbContext _context;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(InventoryDbContext context, ILogger<InventoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task CreateStockItemAsync(Guid productId, string productName, int initialStock, string tenantId)
        {
            // Check if stock record already exists
            var exists = await _context.StockItems
                .AnyAsync(s =>
                    s.ProductId == productId &&
                    s.TenantId == tenantId);

            if (exists)
            {
                _logger.LogWarning(
                    "Stock record already exists for ProductId {ProductId}",
                    productId);
                return;
            }

            _context.StockItems.Add(new StockItem
            {
                ProductId = productId,
                ProductName = productName,
                TenantId = tenantId,
                QuantityAvailable = initialStock,
                QuantityReserved = 0,
            });

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Stock record created: ProductId={ProductId} " +
                "InitialStock={Stock}",
                productId, initialStock);
        }

        public async Task<ApiResponse<StockDto>> GetStockAsync(Guid productId, string tenantId)
        {
            var stock = await _context.StockItems
                    .FirstOrDefaultAsync(s => s.ProductId == productId && s.TenantId == tenantId);

            if (stock is null)
            {
                return ApiResponse<StockDto>.Fail("Stock record not found");
            }

            return ApiResponse<StockDto>.Ok(MapToDto(stock));

        }

        public async Task<ApiResponse<bool>> ReleaseStockAsync(Guid orderId, string tenantId)
        {
            _logger.LogInformation(
             "Releasing stock for cancelled order {OrderId}",
             orderId);

            // In a full implementation we would track which items
            // were reserved per order and release exactly those.
            // For simplicity we just log here.
            // TODO: Store reserved items in a separate table.

            return ApiResponse<bool>.Ok(true);
        }


        /// <summary>
        /// Reserves stock for an order — Saga Step 3.
        ///
        /// IDEMPOTENCY:
        /// Checks if reservation already exists for this OrderId.
        /// If yes → return existing result.
        /// If no  → try to reserve.
        ///
        /// ATOMICITY:
        /// All items must be reservable — if any item has insufficient
        /// stock, the entire reservation fails (no partial reservation).
        /// </summary>
        public async Task<ApiResponse<bool>> ReserveStockAsync(Guid orderId, List<(Guid productId, int quantity)> items, string tenantId)
        {
            var existingReservation = await _context.stockReservations
                 .FirstOrDefaultAsync(r => r.OrderId == orderId);

            if (existingReservation != null)
            {
                _logger.LogWarning("Duplicate reservation for OrderId {OrderId} - " + "returning existing result", orderId);

                return existingReservation.Success ? ApiResponse<bool>.Ok(true) : ApiResponse<bool>.Fail(existingReservation.FailureReason ?? "Insufficient stock");


            }

            var productIds = items.Select(i => i.productId).ToList();
            var stockItems = await _context.StockItems
                .Where(s => productIds.Contains(s.ProductId) && s.TenantId == tenantId)
                .ToListAsync();

            foreach (var (productId,quantity) in items)
            {
                var stock = await _context.StockItems
                    .FirstOrDefaultAsync(s => s.ProductId == productId);

                if(stock is null)
                {
                    var reservation = new StockReservation
                    {
                        OrderId = orderId,
                        TenantId = tenantId,
                        Success = false,
                        FailureReason = $"Product{productId} not found in inventory"
                    };
                    _context.stockReservations.Add(reservation);
                    await _context.SaveChangesAsync();

                    return ApiResponse<bool>.Fail(reservation.FailureReason);
                }

                if (stock.QuantityAvailable < quantity)
                {
                    var reason = $"Insufficient stock for {stock.ProductName}. " + $"Available: {stock.QuantityAvailable}, " + $"Requested: {quantity}";

                    var reservation = new StockReservation
                    {
                        OrderId = orderId,
                        TenantId = tenantId,
                        Success = false,
                        FailureReason = reason,
                    };

                    _context.stockReservations.Add(reservation);
                    await _context.SaveChangesAsync();

                    _logger.LogWarning("Stock reservation failed for OrderId {OrderId}: {Reason}", orderId, reason);

                    return ApiResponse<bool>.Fail(reason);
                }
            }
            foreach (var (productId, quantity) in items)
            {
                var stock = stockItems
                    .First(s => s.ProductId == productId);

                stock.QuantityAvailable -= quantity;
                stock.QuantityReserved += quantity;
                stock.UpdatedAt = DateTime.UtcNow;
            }

            // Record successful reservation
            _context.stockReservations.Add(new StockReservation
            {
                OrderId = orderId,
                TenantId = tenantId,
                Success = true,
            });

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Stock reserved for OrderId {OrderId} " +
                "Items {ItemCount} Tenant {TenantId}",
                orderId, items.Count, tenantId);

            return ApiResponse<bool>.Ok(true);
        }

        public async Task<ApiResponse<StockDto>> UpdateStockAsync(Guid productId, int quantity, string tenantId)
        {
            if (quantity < 0)
                return ApiResponse<StockDto>.Fail("Quantity cannot be negative");

            var stock = await _context.StockItems
                .FirstOrDefaultAsync(s => s.ProductId == productId && s.TenantId == tenantId);

            if (stock is null)
                return ApiResponse<StockDto>.Fail("Stock record not found");

            stock.QuantityAvailable = quantity;
            stock.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Stock updated: ProductId ={ProductId} " + "NewQuantity = {Quantity} Tenant = {TenantId}", productId, quantity, tenantId);

            return ApiResponse<StockDto>.Ok(MapToDto(stock));
        }

        private static StockDto MapToDto(StockItem s) => new()
        {
            ProductId = s.ProductId,
            ProductName = s.ProductName,
            QuantityAvailable = s.QuantityAvailable,
            QuantityReserved = s.QuantityReserved,
            QuantityTotal = s.QuantityAvailable + s.QuantityReserved,
            UpdatedAt = s.UpdatedAt,
        };
    }
}
