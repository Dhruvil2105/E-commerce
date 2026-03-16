using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECommerce.Shared.Message;

namespace ECommerce.Shared.Events
{
    /// <summary>
    /// All events published by the Inventory Service.
    ///
    /// WHO PUBLISHES: Inventory Service
    /// WHO CONSUMES:
    ///   - StockReservedEvent  → Order Service (Saga step result)
    ///   - StockReleasedEvent  → internal confirmation only
    /// </summary>

    // ────────────────────────────────────────────────────────────
    // Event: StockReservedEvent
    // Fired after Inventory Service attempts to reserve stock
    // for an order. One event covers both success and failure.
    //
    // SAGA STEP:
    // Order Service publishes OrderPlacedEvent
    //   → Inventory Service tries to reserve stock
    //   → publishes StockReservedEvent (success or failure)
    //   → Order Service receives it and decides next step
    // ────────────────────────────────────────────────────────────
    public record StockReservedEvent : BaseMessage
    {
        public Guid OrderId { get; init; }

        /// <summary>
        /// true  = stock reserved successfully, saga can continue.
        /// false = not enough stock, saga must rollback.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// Only populated when Success = false.
        /// Example: "Insufficient stock for product prd_abc123"
        /// </summary>
        public string? FailureReason { get; init; }
    }

    // ────────────────────────────────────────────────────────────
    // Event: StockReleasedEvent
    // This is a COMPENSATING TRANSACTION event.
    //
    // WHEN IS IT USED:
    // If payment fails AFTER stock was already reserved:
    //   Step 1: Order placed         ✓
    //   Step 2: Stock reserved       ✓
    //   Step 3: Payment charged      ✗ FAILED
    //
    // Now we must UNDO step 2 — release the reserved stock
    // back to available inventory.
    //
    // Order Service publishes this event as a compensating action.
    // Inventory Service consumes it and releases the reservation.
    // ────────────────────────────────────────────────────────────
    public record StockReleasedEvent : BaseMessage
    {
        /// <summary>
        /// Which order's stock reservation should be released.
        /// Inventory Service finds all reservations for this
        /// OrderId and puts the stock back.
        /// </summary>
        public Guid OrderId { get; init; }
    }
}
