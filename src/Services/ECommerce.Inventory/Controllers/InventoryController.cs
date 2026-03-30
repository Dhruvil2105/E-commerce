using ECommerce.Inventory.DTOs;
using ECommerce.Inventory.Services;
using ECommerce.Shared.DTOs;
using ECommerce.Shared.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Inventory.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly CurrentUser _currentUser;

    public InventoryController(
        IInventoryService inventoryService,
        CurrentUser currentUser)
    {
        _inventoryService = inventoryService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// GET /api/inventory/{productId}
    /// Returns current stock level for a product.
    /// </summary>
    [HttpGet("{productId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<StockDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<StockDto>), 404)]
    public async Task<ActionResult<ApiResponse<StockDto>>> GetStock(
        Guid productId)
    {
        var tenantId = string.IsNullOrEmpty(_currentUser.TenantId)
            ? "default" : _currentUser.TenantId;

        var result = await _inventoryService
            .GetStockAsync(productId, tenantId);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// PUT /api/inventory/{productId}
    /// Updates stock quantity. Admin only.
    /// Used when new shipment arrives.
    /// </summary>
    [HttpPut("{productId:guid}")]
    [AdminOnly]
    [ProducesResponseType(typeof(ApiResponse<StockDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<StockDto>), 400)]
    [ProducesResponseType(typeof(ApiResponse<StockDto>), 403)]
    [ProducesResponseType(typeof(ApiResponse<StockDto>), 404)]
    public async Task<ActionResult<ApiResponse<StockDto>>> UpdateStock(
        Guid productId,
        [FromBody] UpdateStockRequest request)
    {
        var result = await _inventoryService
            .UpdateStockAsync(productId, request.Quantity,
                _currentUser.TenantId);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}