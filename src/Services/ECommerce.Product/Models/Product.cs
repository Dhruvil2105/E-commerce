namespace ECommerce.Product.Models
{
    /// <summary>
    /// Product entity stored in ecommerce_product database.
    /// No other service can access this table directly.
    /// Image is stored as bytes directly in PostgreSQL.
    /// </summary>
    public class Product
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Raw image bytes stored in PostgreSQL bytea column.
        /// Null if no image uploaded yet.
        /// Max 5MB enforced in ProductService.
        /// </summary>
        public byte[]? ImageData { get; set; }

        /// <summary>
        /// MIME type of uploaded image.
        /// Examples: "image/jpeg", "image/png", "image/webp"
        /// Used as Content-Type when serving the image.
        /// </summary>
        public string? ImageContentType { get; set; }

        /// <summary>
        /// Original filename of uploaded image.
        /// Example: "iphone-15-pro.jpg"
        /// </summary>
        public string? ImageFileName { get; set; }

        /// <summary>
        /// Soft delete flag.
        /// false = product hidden from catalogue.
        /// We never hard delete — orders reference product IDs.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Multi-tenant isolation.
        /// Every DB query MUST filter by TenantId.
        /// </summary>
        public string TenantId { get; set; } = "default";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
