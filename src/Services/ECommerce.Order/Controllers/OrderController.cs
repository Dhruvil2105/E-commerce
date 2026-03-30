using ECommerce.Order.DTOs;
using ECommerce.Order.Interface;
using ECommerce.Shared.DTOs;
using ECommerce.Shared.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Order.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly CurrentUser _currentUser;


        public OrderController(IOrderService orderService, CurrentUser currentUser)
        {
            _orderService = orderService;
            _currentUser = currentUser;
        }

        /// <summary>
        /// POST /api/orders
        /// Places a new order and starts the Saga.
        /// Returns immediately with order in PENDING status.
        /// Status changes to CONFIRMED or CANCELLED asynchronously.
        /// </summary>
        [HttpPost]
        [RequireAuth]
        [ProducesResponseType(typeof(ApiResponse<OrderDto>),201)]
        [ProducesResponseType(typeof(ApiResponse<OrderDto>),400)]
        [ProducesResponseType(typeof(ApiResponse<OrderDto>),401)]
        public async Task<ActionResult<ApiResponse<OrderDto>>> PlaceOrder([FromBody] PlaceOrderRequest request)
        {
            var result = await _orderService.PlaceOrderAsync(request, _currentUser.UserId, _currentUser.TenantId);

            if (!result.Success)
                return BadRequest(result);

            return StatusCode(201, result);
        }

        /// <summary>
        /// GET /api/orders
        /// Returns all orders for the current user.
        /// </summary>
        [HttpGet]
        [RequireAuth]
        [ProducesResponseType(typeof(ApiResponse<List<OrderDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<List<OrderDto>>), 401)]
        public async Task<ActionResult<ApiResponse<List<OrderDto>>>> GetMyOrders()
        {
            var result = await _orderService.GetMyOrdersAsync(
                _currentUser.UserId,
                _currentUser.TenantId);

            return Ok(result);
        }

        /// <summary>
        /// GET /api/orders/{id}
        /// Returns a single order by ID.
        /// Only returns the order if it belongs to the current user.
        /// </summary>
        [HttpGet("{id:guid}")]
        [RequireAuth]
        [ProducesResponseType(typeof(ApiResponse<OrderDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<OrderDto>), 401)]
        [ProducesResponseType(typeof(ApiResponse<OrderDto>), 404)]
        public async Task<ActionResult<ApiResponse<OrderDto>>> GetById(Guid id)
        {
            var result = await _orderService.GetByIdAsync(
                id,
                _currentUser.UserId,
                _currentUser.TenantId);

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }
    }
}
