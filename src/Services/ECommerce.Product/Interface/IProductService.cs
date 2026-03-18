using ECommerce.Product.DTOs;
using ECommerce.Shared.DTOs;

namespace ECommerce.Product.Interface
{
    public interface IProductService
    {
        /// <summary>Get all active products. Optional category filter.</summary>
        Task<ApiResponse<List<ProductDto>>> GetAllAsync(
            string tenantId,
            string? category);

        /// <summary>Get single product by ID.</summary>
        Task<ApiResponse<ProductDto>> GetByIdAsync(
            Guid id,
            string tenantId);

        /// <summary>
        /// Get raw image bytes for a product.
        /// Returns (null, null, null) if no image.
        /// </summary>
        Task<(byte[]? data, string? contentType, string? fileName)> GetImageAsync(
            Guid id,
            string tenantId);

        /// <summary>Create a new product. Image is optional.</summary>
        Task<ApiResponse<ProductDto>> CreateAsync(
            CreateProductRequest request,
            string tenantId,
            IFormFile? image);

        /// <summary>Update existing product. Image is optional.</summary>
        Task<ApiResponse<ProductDto>> UpdateAsync(
            Guid id,
            UpdateProductRequest request,
            string tenantId,
            IFormFile? image);

        /// <summary>Soft delete — sets IsActive = false.</summary>
        Task<ApiResponse<bool>> DeleteAsync(
            Guid id,
            string tenantId);
    }
}
