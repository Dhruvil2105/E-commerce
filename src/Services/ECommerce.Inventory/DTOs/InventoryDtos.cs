namespace ECommerce.Inventory.DTOs
{
    public record StockDto
    {
        public Guid ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public int QuantityAvailable { get; init; }
        public int QuantityReserved { get; init; }
        public int QuantityTotal { get; init; }
        public DateTime UpdatedAt { get; init; }
    }
    public record UpdateStockRequest
    {
        /// <summary>New available quantity to set.</summary>
        public int Quantity { get; init; }
    }
}
