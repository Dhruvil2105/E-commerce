namespace ECommerce.Order.Models
{
    /// <summary>
    /// Represents a customer order.
    ///
    /// SAGA STATE TRACKING:
    /// The order tracks the Saga progress using two boolean flags:
    ///   PaymentProcessed — has Payment Service responded?
    ///   StockReserved    — has Inventory Service responded?
    ///
    /// Order Service waits for BOTH responses.
    /// When both arrive → decide to CONFIRM or CANCEL.
    ///
    /// DATABASE OWNERSHIP:
    /// This entity lives ONLY in ecommerce_order database.
    /// Payment and Inventory Services never access this table.
    /// They communicate only via RabbitMQ events.
    /// </summary>
    public class Order
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Who placed the order.
        /// Comes from CurrentUser.UserId (X-User-Id header).
        /// Used to show customer their own orders.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Multi-tenant isolation.
        /// Every query filters by TenantId.
        /// </summary>
        public string TenantId { get; set; } = "default";

        /// <summary>
        /// Current state of the order.
        /// Changes as Saga progresses.
        /// </summary>
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        /// <summary>
        /// Total price of all items.
        /// Calculated when order is placed — not recalculated later.
        /// Price is locked at time of order even if product price changes.
        /// </summary>
        public decimal Total { get; set; }

        /// <summary>
        /// Why was the order cancelled?
        /// Examples:
        ///   "Payment declined — insufficient funds"
        ///   "Insufficient stock for iPhone 15 Pro"
        /// Only populated when Status = Cancelled.
        /// </summary>
        public string? CancellationReason { get; set; }

        // ── Saga tracking flags ───────────────────────────────────
        // Order Service waits for responses from both
        // Payment Service and Inventory Service.
        // These flags track which responses have arrived.

        /// <summary>
        /// Has Payment Service responded to OrderPlacedEvent?
        /// false = still waiting
        /// true  = PaymentProcessedEvent received
        /// </summary>
        public bool PaymentProcessed { get; set; } = false;

        /// <summary>
        /// Did the payment succeed?
        /// Only meaningful when PaymentProcessed = true.
        /// </summary>
        public bool PaymentSuccess { get; set; } = false;

        /// <summary>
        /// Has Inventory Service responded to OrderPlacedEvent?
        /// false = still waiting
        /// true  = StockReservedEvent received
        /// </summary>
        public bool StockReserved { get; set; } = false;

        /// <summary>
        /// Did the stock reservation succeed?
        /// Only meaningful when StockReserved = true.
        /// </summary>
        public bool StockSuccess { get; set; } = false;

        // ── Shipping info ─────────────────────────────────────────

        public string? TrackingNumber { get; set; }
        public string? Courier { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation property — EF Core loads order items.
        /// One order has many items.
        /// </summary>
        public List<OrderItem> Items { get; set; } = [];
    }
}