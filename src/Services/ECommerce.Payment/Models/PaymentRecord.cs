using System.Security.Principal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace ECommerce.Payment.Models
{
    /// <summary>
    /// Stores every payment attempt in the system.
    ///
    /// WHY STORE ALL ATTEMPTS:
    /// A payment can be attempted multiple times due to retries.
    /// Storing every attempt lets us:
    ///   1. Detect duplicate charges (idempotency)
    ///   2. Show full payment history
    ///   3. Process refunds using the ChargeId
    ///   4. Audit trail for financial compliance
    ///
    /// IDEMPOTENCY KEY:
    /// OrderId is our idempotency key.
    /// If we receive the same OrderPlacedEvent twice —
    /// we check if a payment record already exists for this OrderId.
    /// If yes → skip, return existing result.
    /// If no → process payment.
    /// This prevents charging the customer twice.
    /// </summary>
    public class PaymentRecord
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Which order this payment belongs to.
        /// Also serves as our idempotency key.
        /// One order = one payment record (enforced by unique index).
        /// </summary>
        public Guid OrderId { get; set; }

        /// <summary>
        /// Who was charged.
        /// Comes from the OrderPlacedEvent.
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        public string TenentId { get; set; } = "default";

        /// <summary>
        /// Amount charged in smallest currency unit.
        /// Example: 129999 = ₹129,999
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Did the payment succeed?
        /// true  = customer was charged successfully
        /// false = payment failed (declined, timeout, etc.)
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Transaction ID from payment gateway (Stripe, Razorpay etc).
        /// Used for refunds and reconciliation.
        /// In our simulation we generate a fake ID.
        /// </summary>
        public string? ChargeId { get; set; }

        /// <summary>
        /// Why did the payment fail?
        /// Examples:
        ///   "Card declined — insufficient funds"
        ///   "Card expired"
        ///   "Payment gateway timeout"
        /// Only populated when Success = false.
        /// </summary>
        public string? FailureReason { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
