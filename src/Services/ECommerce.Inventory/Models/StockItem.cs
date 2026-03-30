using System.Security.Principal;

namespace ECommerce.Inventory.Models
{
    /// When Product Service creates a product it publishes
    /// ProductCreatedEvent. Inventory Service consumes it
    /// and creates a StockItem record here.
    /// We never call Product Service directly — loose coupling.
    ///
    /// STOCK LEVELS:
    ///   QuantityAvailable = stock customer can buy right now
    ///   QuantityReserved  = stock held for pending orders
    ///   QuantityTotal     = Available + Reserved
    ///
    /// EXAMPLE:
    ///   100 units in warehouse
    ///   10 units reserved for pending orders
    ///   QuantityAvailable = 90
    ///   QuantityReserved  = 10
    ///   QuantityTotal     = 100
    /// </summary>
    public class StockItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string TenantId { get; set; } = "Default";
        
        /// <summary>
        /// Units available for purchase right now.
        /// Decreases when stock is reserved.
        /// Increases when reservation is released.
        /// </summary
        public int QuantityAvailable { get; set; }

        /// <summary>
        /// Units held for pending orders (Saga in progress).
        /// Increases when stock is reserved.
        /// Decreases when order is confirmed or cancelled.
        /// </summary>
        public int QuantityReserved { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
