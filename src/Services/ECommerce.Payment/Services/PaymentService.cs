using ECommerce.Payment.Data;
using ECommerce.Payment.DTOs;
using ECommerce.Payment.Models;
using ECommerce.Shared.DTOs;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Payment.Services
{
    /// <summary>
    /// Handles payment processing with full idempotency.
    ///
    /// SIMULATION:
    /// We do not integrate a real payment gateway here.
    /// Instead we simulate:
    ///   - 90% of payments succeed
    ///   - 10% fail (simulates declined cards)
    ///
    /// In production replace SimulatePaymentGateway()
    /// with real Stripe/Razorpay/PayU API calls.
    /// </summary>
    public class PaymentService : IPaymentService
    {
        private readonly PaymentDbContext _context;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(PaymentDbContext context, ILogger<PaymentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ApiResponse<PaymentDto>> GetByOrderIdAsync(Guid orderId, string tenantId)
        {
            var payment = await _context.payments.FirstOrDefaultAsync(p => p.OrderId == orderId && p.TenentId == tenantId);

            if (payment is null)
                return ApiResponse<PaymentDto>.Fail("Payment not found");

            return ApiResponse<PaymentDto>.Ok(MapToDto(payment));
        }

        /// <summary>
        /// Process payment for an order.
        ///
        /// IDEMPOTENCY:
        /// Checks if a payment record already exists for this OrderId.
        /// If yes → return existing result (no double charge).
        /// If no  → process new payment.
        ///
        /// This handles the case where:
        ///   - OrderPlacedEvent is delivered twice by RabbitMQ
        ///   - Network retry causes duplicate processing
        /// </summary>
        public async Task<ApiResponse<PaymentDto>> ProcessPaymentAsync(Guid orderId, string userId, string tenantId, decimal amount)
        {
            var existingPayment = await _context.payments.FirstOrDefaultAsync(p => p.OrderId == orderId);

            if (existingPayment != null)
            {
                _logger.LogWarning("Duplicate payment for OderId {OrderId} - " + "returning existing result", orderId);

                return ApiResponse<PaymentDto>.Ok(MapToDto(existingPayment));
            }

            _logger.LogInformation("Processing payment for OrderId {OrderId} " + "Amount {Amount} UserId {UserId}", orderId, amount, userId);

            var payment = new PaymentRecord
            {
                OrderId = orderId,
                UserId = userId,
                TenentId = tenantId,
                Amount = amount,
                Status = "Processing",
            };

            _context.payments.Add(payment);
            await _context.SaveChangesAsync();

            var (sucess, chargeId, failureReason) = await SimulatePaymentGatway(orderId, amount);

            payment.Success = sucess;
            payment.ChargeId = chargeId;
            payment.FailureReason = failureReason;
            payment.Status = sucess ? "Succeeded" : "Failed";
            payment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
           "Payment {Status} for OrderId {OrderId} ChargeId {ChargeId}",
           payment.Status, orderId, chargeId);

            return ApiResponse<PaymentDto>.Ok(MapToDto(payment));

        }

        // ── Private helpers ───────────────────────────────────────

        /// <summary>
        /// Simulates calling a real payment gateway.
        ///
        /// SIMULATION RULES:
        ///   Amount >= 1,000,000  → always fails (too large)
        ///   Normal amount        → 90% succeed, 10% fail randomly
        ///
        /// REPLACE THIS in production with:
        ///   Stripe:    StripeClient.PaymentIntents.Create(...)
        ///   Razorpay:  razorpayClient.Order.Create(...)
        ///   PayU:      PayUClient.Charge(...)
        /// </summary>
        private static async Task<(bool success, string? chargeId, string? faolureReason)> SimulatePaymentGatway(Guid orderId, decimal amount)
        {
            // Simulate network delay (real gateway takes 200-800ms)
            await Task.Delay(Random.Shared.Next(200, 500));

            if (amount >= 1_000_000)
                return (false, null, "Amount exceeds transaction limit");

            var random = Random.Shared.NextDouble();

            if (random > 0.1)
            {
                var chargeId = $"ch_{orderId:N}".Substring(0, 24);
                return (true, chargeId, null);
            }
            else
            {
                var reasons = new[]
                {
                    "Card declined — insufficient funds",
                    "Card expired",
                    "Transaction blocked by bank",
                    "Invalid card number",
                };

                var reason = reasons[Random.Shared.Next(reasons.Length)];
                return (false, null, reason);
            }
        }

        private static PaymentDto MapToDto(PaymentRecord p) => new()
        {
            Id = p.Id,
            OrderId = p.OrderId,
            Amount = p.Amount,
            Success = p.Success,
            ChargeId = p.ChargeId,
            FailurReason = p.FailureReason,
            Status = p.Status,
            CreatedAt = p.CreatedAt,
        };
    }
}
