using ECommerce.Order.DTOs;
using ECommerce.Shared.DTOs;

namespace ECommerce.Order.Interface
{
    public interface IOrderService
    {
        Task<ApiResponse<OrderDto>> PlaceOrderAsync(PlaceOrderRequest request, string userId, string tenantId);
        Task<ApiResponse<List<OrderDto>>> GetMyOrdersAsync(string userId, string tenantId);
        Task<ApiResponse<OrderDto>> GetByIdAsync(Guid id, string userId, string tenantId);
    }
}
