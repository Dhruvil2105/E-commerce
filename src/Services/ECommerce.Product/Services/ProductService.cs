using ECommerce.Product.Data;
using ECommerce.Product.DTOs;
using ECommerce.Product.Interface;
using ECommerce.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Product.Services
{
    public class ProductService : IProductService
    {
        private readonly ProductDbContext _context;
        private readonly ILogger<ProductService> _logger;

        /// <summary>Maximum allowed image size — 5MB</summary>
        private const int MaxImageBytes = 5 * 1024 * 1024;

        /// <summary>Allowed image MIME types</summary>
        private static readonly string[] AllowedImageTypes =
            ["image/jpeg", "image/png", "image/webp", "image/gif"];

        public ProductService(ProductDbContext context, ILogger<ProductService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<ApiResponse<List<ProductDto>>> GetAllAsync(string tenantId, string? category)
        {
            var query = _context.Products
                .Where(p => p.TenantId == tenantId && p.IsActive);

            // Optional category filter
            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(p => p.Category == category);

            var products = await query
                .OrderBy(p => p.Name)
                .Select(p => MapToDto(p))
                .ToListAsync();

            _logger.LogInformation("GetAll: returned {Count} products for tenant {TenantId}", products.Count, tenantId);

            return ApiResponse<List<ProductDto>>.Ok(products);
        }

        /// <inheritdoc/>
        public async Task<ApiResponse<ProductDto>> GetByIdAsync(Guid id, string tenantId)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.Id == id &&
                    p.TenantId == tenantId &&
                    p.IsActive == true);

            if (product is null)
            {
                _logger.LogWarning("Product not found: {ProductId} for tenant {TenantId}", id, tenantId);

                return ApiResponse<ProductDto>.Fail("Product not found");
            }

            return ApiResponse<ProductDto>.Ok(MapToDto(product));
        }

        /// <inheritdoc/>
        public async Task<(byte[]? data, string? contentType, string? fileName)> GetImageAsync(Guid id, string tenantId)
        {
            // Select only image columns — avoid loading all product data
            var image = await _context.Products
                .Where(p => p.Id == id && p.TenantId == tenantId)
                .Select(p => new
                {
                    p.ImageData,
                    p.ImageContentType,
                    p.ImageFileName
                })
                .FirstOrDefaultAsync();

            if (image?.ImageData is null)
                return (null, null, null);

            return (image.ImageData, image.ImageContentType, image.ImageFileName);
        }

        /// <inheritdoc/>
        public async Task<ApiResponse<ProductDto>> CreateAsync(CreateProductRequest request, string tenantId, IFormFile? image)
        {
            // Validate request fields
            if (string.IsNullOrWhiteSpace(request.Name))
                return ApiResponse<ProductDto>.Fail("Product name is required");

            if (request.Price <= 0)
                return ApiResponse<ProductDto>.Fail(
                    "Price must be greater than zero");

            if (string.IsNullOrWhiteSpace(request.Category))
                return ApiResponse<ProductDto>.Fail("Category is required");

            // Validate image if provided
            var imageValidation = ValidateImage(image);
            if (imageValidation != null)
                return ApiResponse<ProductDto>.Fail(imageValidation);

            // Build the entity
            var product = new Models.Product
            {
                Name = request.Name.Trim(),
                Description = request.Description.Trim(),
                Price = request.Price,
                Category = request.Category.Trim(),
                TenantId = tenantId,
            };

            // Store image if provided
            if (image != null)
                await AttachImageAsync(product, image);

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Product created: {ProductId} {Name} HasImage: {HasImage} " +
                "Tenant: {TenantId}",
                product.Id, product.Name,
                product.ImageData != null, tenantId);

            return ApiResponse<ProductDto>.Ok(MapToDto(product));
        }

        /// <inheritdoc/>
        public async Task<ApiResponse<ProductDto>> UpdateAsync(Guid id, UpdateProductRequest request, string tenantId, IFormFile? image)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.Id == id && p.TenantId == tenantId);

            if (product is null)
                return ApiResponse<ProductDto>.Fail("Product not found");

            // Only update fields that are provided — null means keep existing
            if (request.Name != null)
                product.Name = request.Name.Trim();

            if (request.Description != null)
                product.Description = request.Description.Trim();

            if (request.Price != null)
            {
                if (request.Price <= 0)
                    return ApiResponse<ProductDto>.Fail("Price must be greater than zero");

                product.Price = request.Price.Value;
            }

            if (request.Category != null)
                product.Category = request.Category.Trim();

            if (request.IsActive != null)
                product.IsActive = request.IsActive.Value;

            // Update image if new one is provided
            if (image != null)
            {
                var imageValidation = ValidateImage(image);
                if (imageValidation != null)
                    return ApiResponse<ProductDto>.Fail(imageValidation);

                await AttachImageAsync(product, image);
            }

            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Product updated: {ProductId} Tenant: {TenantId}", product.Id, tenantId);

            return ApiResponse<ProductDto>.Ok(MapToDto(product));
        }

        /// <inheritdoc/>
        public async Task<ApiResponse<bool>> DeleteAsync(Guid id, string tenantId)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.Id == id && p.TenantId == tenantId);

            if (product is null)
                return ApiResponse<bool>.Fail("Product not found");

            // Soft delete — never hard delete products
            // Orders reference product IDs — deleting would break history
            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Product soft-deleted: {ProductId} Tenant: {TenantId}",
                product.Id, tenantId);

            return ApiResponse<bool>.Ok(true);
        }

        // ── Private helpers ───────────────────────────────────────────────

        /// <summary>
        /// Validates the uploaded image.
        /// Returns an error message if invalid, null if valid.
        /// </summary>
        private static string? ValidateImage(IFormFile? image)
        {
            if (image == null) return null;

            if (image.Length == 0)
                return "Image file is empty";

            if (image.Length > MaxImageBytes)
                return $"Image too large. Maximum size is 5MB. " +
                       $"Your file is {image.Length / 1024 / 1024}MB";

            if (!AllowedImageTypes.Contains(image.ContentType.ToLower()))
                return $"Invalid image type '{image.ContentType}'. " +
                       $"Allowed types: JPEG, PNG, WebP, GIF";

            return null;
        }

        /// <summary>
        /// Reads image bytes from IFormFile and attaches to product.
        /// </summary>
        private static async Task AttachImageAsync(
            Models.Product product, IFormFile image)
        {
            using var ms = new MemoryStream();
            await image.CopyToAsync(ms);

            product.ImageData = ms.ToArray();
            product.ImageContentType = image.ContentType;
            product.ImageFileName = image.FileName;
        }

        /// <summary>
        /// Maps Product entity to ProductDto.
        /// Note: ImageData bytes are NOT included in the DTO.
        /// </summary>
        private static ProductDto MapToDto(Models.Product p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Category = p.Category,
            HasImage = p.ImageData != null,
            ImageFileName = p.ImageFileName,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
        };
    }
}
