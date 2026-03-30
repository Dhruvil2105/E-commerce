namespace ECommerce.Inventory.Models
{
    /// <summary>
    /// Tracks every stock reservation for idempotency.
    ///
    /// WHY THIS EXISTS:
    /// When Inventory Service receives OrderPlacedEvent twice
    /// (RabbitMQ retry), it checks this table:
    ///   "Does a reservation already exist for this OrderId?"
    ///   Yes → return existing result (do not reserve again)
    ///   No  → create new reservation
    ///
    /// This prevents reducing stock twice for the same order.
    /// </summary>
    public class StockReservation
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid OrderId { get; set; }
        public string TenantId { get; set; } = "default";

        public bool Success { get; set; }

        public string? FailureReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
