namespace ECommerce.Product.DTOs
{
    /// <summary>
    /// Returned when listing or fetching a product.
    /// Does NOT include ImageData bytes — too large for list responses.
    /// Client fetches image separately via GET /api/products/{id}/image
    /// </summary>
    public record ProductDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public string Category { get; init; } = string.Empty;

        /// <summary>
        /// true = product has an image, call /api/products/{id}/image
        /// false = no image uploaded yet
        /// </summary>
        public bool HasImage { get; init; }
        public string? ImageFileName { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
    }

    /// <summary>
    /// Request body for creating a product.
    /// Sent as multipart/form-data so image file can be included.
    /// </summary>
    public record CreateProductRequest
    {
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public string Category { get; init; } = string.Empty;
        /// <summary>Product image — JPEG, PNG, WebP, GIF (max 5MB)</summary>
        public IFormFile? Image { get; set; }
    }

    /// <summary>
    /// Request body for updating a product.
    /// All fields are optional — null means keep existing value.
    /// </summary>
    public record UpdateProductRequest
    {
        public string? Name { get; init; }
        public string? Description { get; init; }
        public decimal? Price { get; init; }
        public string? Category { get; init; }
        public bool? IsActive { get; init; }
        /// <summary>New product image (optional)</summary>
        public IFormFile? Image { get; set; }
    }
}
