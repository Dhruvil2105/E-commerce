namespace ECommerce.Order.Models
{
    /// <summary>
    /// Represents all possible states an order can be in.
    ///
    /// STATE MACHINE:
    ///   PENDING
    ///     │
    ///     ├── Payment fails   → CANCELLED
    ///     ├── Stock fails     → CANCELLED
    ///     │
    ///     └── Both succeed   → CONFIRMED
    ///                              │
    ///                              └── SHIPPED → DELIVERED
    ///
    /// Each transition is triggered by a RabbitMQ event.
    /// The order status always reflects what actually happened.
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>
        /// Order created — waiting for payment and stock confirmation.
        /// This is the initial state when customer places an order.
        /// Saga steps are running in the background.
        /// </summary>
        Pending = 1,

        /// <summary>
        /// Payment charged AND stock reserved successfully.
        /// Saga completed successfully.
        /// Order is being prepared for shipment.
        /// </summary>
        Confirmed = 2,

        /// <summary>
        /// Order cancelled — either payment failed or stock unavailable.
        /// Compensating transactions have already run.
        /// Customer is NOT charged.
        /// </summary>
        Cancelled = 3,

        /// <summary>
        /// Order has been shipped by the warehouse.
        /// Tracking number is available.
        /// </summary>
        Shipped = 4,

        /// <summary>
        /// Order delivered to the customer.
        /// Final state — no further transitions possible.
        /// </summary>
        Delivered = 5,
    }
}

