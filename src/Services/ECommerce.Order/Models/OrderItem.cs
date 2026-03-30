namespace ECommerce.Order.Models
{
    /// <summary>
    /// Represents a single product line in an order.
    ///
    /// WHY STORE PRICE HERE:
    /// Product prices can change after an order is placed.
    /// We store the price at the time of ordering — not the current price.
    /// This preserves the exact amount the customer agreed to pay.
    ///
    /// WHY STORE PRODUCT NAME HERE:
    /// If a product is deleted or renamed, the order history
    /// must still show what the customer actually ordered.
    /// </summary>
    public class OrderItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Foreign key — which order this item belongs to.</summary>
        public Guid OrderId { get; set; }

        /// <summary>
        /// Which product was ordered.
        /// References Product Service — but we never JOIN across services.
        /// We just store the ID for reference.
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Product name at time of ordering.
        /// Stored here so order history is accurate even if
        /// product is renamed or deleted later.
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        /// <summary>
        /// Price per unit at time of ordering.
        /// Locked — never updated even if product price changes.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Quantity × UnitPrice.
        /// Calculated and stored to avoid recalculation.
        /// </summary>
        public decimal Subtotal { get; set; }

        /// <summary>Navigation property back to parent order.</summary>
        public Order Order { get; set; } = null!;
    }
}
