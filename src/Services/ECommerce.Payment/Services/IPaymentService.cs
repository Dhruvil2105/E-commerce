using ECommerce.Payment.DTOs;
using ECommerce.Shared.DTOs;

namespace ECommerce.Payment.Services
{
    public interface IPaymentService
    {
        /// <summary>
        /// Process payment for an order.
        /// Idempotent — safe to call multiple times for same OrderId.
        /// </summary>
        Task<ApiResponse<PaymentDto>> ProcessPaymentAsync(Guid orderId, string userId, string tenantId, decimal amount);

        /// <summary>Get payment record for an order.</summary>
        Task<ApiResponse<PaymentDto>> GetByOrderIdAsync(Guid orderId, string tenantId);
    }
}
