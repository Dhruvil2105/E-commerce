using ECommerce.Product.DTOs;
using ECommerce.Product.Interface;
using ECommerce.Shared.DTOs;
using ECommerce.Shared.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Product.Controllers
{
    /// <summary>
    /// Product catalogue endpoints.
    ///
    /// Public (no auth):
    ///   GET /api/products
    ///   GET /api/products?category=Electronics
    ///   GET /api/products/{id}
    ///   GET /api/products/{id}/image
    ///
    /// Admin only:
    ///   POST   /api/products
    ///   PUT    /api/products/{id}
    ///   DELETE /api/products/{id}
    /// </summary>

    [Route("api/products")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly CurrentUser _currentUser;

        public ProductController(IProductService productService, CurrentUser currentUser)
        {
            _productService = productService;
            _currentUser = currentUser;
        }

        // ── Public endpoints ──────────────────────────────────────────────

        /// <summary>
        /// GET /api/products
        /// GET /api/products?category=Electronics
        ///
        /// Returns all active products for the current tenant.
        /// No authentication required — public product catalogue.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<ProductDto>>), 200)]
        public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetAll(
            [FromQuery] string? category)
        {
            // Use default tenant if no auth headers present
            var tenantId = string.IsNullOrEmpty(_currentUser.TenantId)
                ? "default" : _currentUser.TenantId;

            var result = await _productService.GetAllAsync(tenantId, category);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/products/{id}
        ///
        /// Returns a single product by ID.
        /// No authentication required.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 404)]
        public async Task<ActionResult<ApiResponse<ProductDto>>> GetById(Guid id)
        {
            var tenantId = string.IsNullOrEmpty(_currentUser.TenantId)
                ? "default" : _currentUser.TenantId;

            var result = await _productService.GetByIdAsync(id, tenantId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        /// <summary>
        /// GET /api/products/{id}/image
        ///
        /// Returns the raw image bytes for a product.
        /// Browser renders this directly as an image.
        ///
        /// Frontend usage:
        ///   img src="http://localhost:5200/api/products/{id}/image"
        /// </summary>
        [HttpGet("{id:guid}/image")]
        public async Task<IActionResult> GetImage(Guid id)
        {
            var tenantId = string.IsNullOrEmpty(_currentUser.TenantId)
                ? "default" : _currentUser.TenantId;

            var (data, contentType, fileName) =
                await _productService.GetImageAsync(id, tenantId);

            if (data is null)
                return NotFound("No image found for this product");

            // File() sets Content-Type and Content-Disposition headers
            // Browser displays this as an image automatically
            return File(data, contentType ?? "image/jpeg", fileName);
        }

        /// <summary>
        /// POST /api/products
        /// Creates a new product. Admin only.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 401)]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 403)]
        public async Task<ActionResult<ApiResponse<ProductDto>>> Create(
            [FromForm] CreateProductRequest form)
        {
            // Enforce Admin role — only admins can create products
            if (!_currentUser.IsAdmin)
                return StatusCode(403,
                    ApiResponse<ProductDto>.Fail(
                        "Only admins can create products"));

            var request = new CreateProductRequest
            {
                Name = form.Name,
                Description = form.Description,
                Price = form.Price,
                Category = form.Category,
            };

            var result = await _productService.CreateAsync(request, _currentUser.TenantId, form.Image);

            if (!result.Success)
                return BadRequest(result);

            return StatusCode(201, result);
        }

        /// <summary>
        /// PUT /api/products/{id}
        /// Updates an existing product. Admin only.
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 401)]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 403)]
        [ProducesResponseType(typeof(ApiResponse<ProductDto>), 404)]
        public async Task<ActionResult<ApiResponse<ProductDto>>> Update(
            Guid id, [FromForm] UpdateProductRequest form)
        {
            if (!_currentUser.IsAdmin)
                return StatusCode(403,
                    ApiResponse<ProductDto>.Fail(
                        "Only admins can update products"));
            var request = new UpdateProductRequest
            {
                Name = form.Name,
                Description = form.Description,
                Price = form.Price,
                Category = form.Category,
                IsActive = form.IsActive,
            };

            var result = await _productService
                .UpdateAsync(id, request, _currentUser.TenantId, form.Image);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        /// <summary>
        /// DELETE /api/products/{id}
        /// Soft deletes a product. Admin only.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 401)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 403)]
        [ProducesResponseType(typeof(ApiResponse<bool>), 404)]
        public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
        {
            if (!_currentUser.IsAdmin)
                return StatusCode(403,
                    ApiResponse<bool>.Fail(
                        "Only admins can delete products"));

            var result = await _productService
             .DeleteAsync(id, _currentUser.TenantId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

    }
}
