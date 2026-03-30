using ECommerce.Inventory.DTOs;
using ECommerce.Shared.DTOs;

namespace ECommerce.Inventory.Services
{
    public interface IInventoryService
    {
        Task<ApiResponse<StockDto>> GetStockAsync(Guid productId, string tenantId);
        Task<ApiResponse<StockDto>> UpdateStockAsync(Guid productId, int quantity, string tenantId);
        Task<ApiResponse<bool>> ReserveStockAsync(Guid orderId, List<(Guid productId, int quantity)> items, string tenantId);
        Task<ApiResponse<bool>> ReleaseStockAsync(Guid orderId, string tenantId);
        Task CreateStockItemAsync(Guid productId, string productName, int initialStock, string tenantId);
    }
}
