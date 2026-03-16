using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECommerce.Shared.Message;

namespace ECommerce.Shared.Events
{
    /// <summary>
    /// All events published by the Payment Service.
    ///
    /// WHO PUBLISHES: Payment Service
    /// WHO CONSUMES:
    ///   - PaymentProcessedEvent → Order Service (continues or rolls back Saga)
    ///                           → Notification Service (success or failure email)
    /// </summary>

    // ────────────────────────────────────────────────────────────
    // Event: PaymentProcessedEvent
    // Fired after Payment Service attempts to charge the customer.
    // One event covers both success AND failure cases.
    // The Success field tells consumers which case it is.
    //
    // WHY ONE EVENT FOR BOTH?
    // Order Service subscribes to this event and checks Success:
    //   true  → confirm the order, continue Saga
    //   false → cancel the order, run compensating transactions
    // Having one event type is simpler than two separate events.
    // ────────────────────────────────────────────────────────────
    public record PaymentProcessedEvent : BaseMessage
    {
        public Guid OrderId { get; init; }
        public string UserId { get; init; } = string.Empty;
        public decimal Amount { get; init; }

        /// <summary>
        /// Unique ID returned by the payment gateway (Stripe, Razorpay etc).
        /// Stored for reference and refunds.
        ///
        /// IDEMPOTENCY KEY:
        /// If Payment Service receives the same OrderPlacedEvent twice
        /// (RabbitMQ retry), it checks: "does a payment record already
        /// exist for this OrderId?" If yes, skip — don't charge twice.
        /// This ChargeId proves the payment already happened.
        /// </summary>
        public string ChargeId { get; init; } = string.Empty;

        /// <summary>
        /// true  = payment succeeded, order can be confirmed.
        /// false = payment failed, order must be cancelled.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// Only populated when Success = false.
        /// Examples:
        ///   "Card declined — insufficient funds"
        ///   "Card expired"
        ///   "Payment gateway timeout"
        /// Order Service passes this to OrderCancelledEvent.Reason
        /// so the customer knows why their order was cancelled.
        /// </summary>
        public string? FailureReason { get; init; }
    }
}
