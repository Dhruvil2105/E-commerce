using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECommerce.Shared.Message;

namespace ECommerce.Shared.Events
{
    /// <summary>
    /// All events published by the Order Service.
    ///
    /// WHO PUBLISHES: Order Service
    /// WHO CONSUMES:
    ///   - OrderPlacedEvent     → Payment Service, Inventory Service
    ///   - OrderConfirmedEvent  → Notification Service
    ///   - OrderCancelledEvent  → Notification Service
    ///   - OrderShippedEvent    → Notification Service
    ///
    /// NAMING CONVENTION — always past tense:
    ///   OrderPlaced   = the order WAS placed (already happened)
    ///   NOT PlaceOrder = that is a command (not an event)
    ///
    /// Events describe facts that already happened.
    /// Commands tell someone to do something.
    /// These are events — they are facts.
    /// </summary>

    // ────────────────────────────────────────────────────────────
    // Event 1: OrderPlacedEvent
    // Fired when a customer successfully places an order.
    // This kicks off the entire Saga:
    //   Step 1: Order Service creates order → publishes this event
    //   Step 2: Payment Service charges the customer
    //   Step 3: Inventory Service reserves the stock
    //   Step 4: Order Service confirms the order
    // ────────────────────────────────────────────────────────────
    public record OrderEvents : BaseMessage
    {
        /// <summary>
        /// Unique ID of the order that was just created.
        /// All other services use this to reference this order.
        /// </summary>
        public Guid OrderId { get; init; }

        /// <summary>
        /// ID of the customer who placed the order.
        /// Comes from CurrentUser.UserId which came from
        /// the X-User-Id header injected by the gateway.
        /// </summary>
        public string UserId { get; init; } = string.Empty;

        /// <summary>
        /// List of items in this order.
        /// We embed the full item data here so consumers
        /// do not need to call back to Order Service to get it.
        /// This is called a "fat event" — it carries enough
        /// data for consumers to act independently.
        /// </summary>
        public List<OrderItemDto> Items { get; init; } = [];

        /// <summary>
        /// Total price of the order.
        /// Payment Service uses this to charge the customer.
        /// decimal is used for money — NEVER use float or double
        /// for money because they have floating point precision errors.
        /// Example: 0.1 + 0.2 = 0.30000000000000004 in float.
        /// decimal is exact.
        /// </summary>
        public decimal Total { get; init; }
    }


    // ────────────────────────────────────────────────────────────
    // Event 2: OrderConfirmedEvent
    // Fired when ALL saga steps succeed:
    //   Payment charged ✓
    //   Stock reserved  ✓
    // Order is now confirmed and will be fulfilled.
    // ────────────────────────────────────────────────────────────
    public record OrderConfirmedEvent : BaseMessage
    {
        public Guid OrderId { get; init; }
        public string UserId { get; init; } = string.Empty;
        public decimal Total { get; init; }
    }

    // ────────────────────────────────────────────────────────────
    // Event 3: OrderCancelledEvent
    // Fired when the Saga fails at any step.
    // Examples:
    //   - Payment declined
    //   - Item out of stock
    // Compensating transactions have already run at this point.
    // Notification Service sends a cancellation email.
    // ────────────────────────────────────────────────────────────
    public record OrderCancelledEvent : BaseMessage
    {
        public Guid OrderId { get; init; }
        public string UserId { get; init; } = string.Empty;

        /// <summary>
        /// Human-readable reason why the order was cancelled.
        /// Examples:
        ///   "Payment declined — insufficient funds"
        ///   "Item out of stock"
        /// Notification Service includes this in the email.
        /// </summary>
        public string Reason { get; init; } = string.Empty;
    }

    // ────────────────────────────────────────────────────────────
    // Event 4: OrderShippedEvent
    // Fired when the warehouse ships the order.
    // Notification Service sends a shipping confirmation
    // email with the tracking number.
    // ────────────────────────────────────────────────────────────
    public record OrderShippedEvent : BaseMessage
    {
        public Guid OrderId { get; init; }
        public string UserId { get; init; } = string.Empty;
        public string TrackingNumber { get; init; } = string.Empty;

        /// <summary>
        /// Name of the courier company.
        /// Examples: "BlueDart", "FedEx", "DHL"
        /// Included in the shipping notification email.
        /// </summary>
        public string Courier { get; init; } = string.Empty;

    }

    // ────────────────────────────────────────────────────────────
    // Shared DTO used INSIDE order events
    // This is NOT a standalone event — it is a data object
    // embedded inside OrderPlacedEvent.Items list.
    // ────────────────────────────────────────────────────────────
    public record OrderItemDto
    {
        /// <summary>
        /// Which product was ordered.
        /// Inventory Service uses this to find the stock record
        /// and reduce the available quantity.
        /// </summary>
        public Guid ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public int Quantity { get; init; }

        /// <summary>
        /// Price at the time of ordering.
        /// We store this here because product prices can change.
        /// The order must always reflect the price the customer
        /// agreed to — not today's price.
        /// </summary>
        public decimal UnitPrice { get; init; }
    }
}
