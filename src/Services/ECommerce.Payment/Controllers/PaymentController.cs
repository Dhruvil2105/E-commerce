using ECommerce.Payment.DTOs;
using ECommerce.Payment.Services;
using ECommerce.Shared.DTOs;
using ECommerce.Shared.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Payment.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly CurrentUser _currentUser;

    public PaymentController(
        IPaymentService paymentService,
        CurrentUser currentUser)
    {
        _paymentService = paymentService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// GET /api/payments/order/{orderId}
    /// Returns payment details for a specific order.
    /// </summary>
    [HttpGet("order/{orderId:guid}")]
    [RequireAuth]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), 404)]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> GetByOrderId(
        Guid orderId)
    {
        var result = await _paymentService
            .GetByOrderIdAsync(orderId, _currentUser.TenantId);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}