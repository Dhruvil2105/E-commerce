using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Shared.DTOs
{
    /// <summary>
    /// Standard response envelope used by EVERY endpoint
    /// across ALL 6 services in this system.
    ///
    /// WHY THIS EXISTS:
    /// Without this, every service returns data differently.
    /// One service returns { "data": {...} }
    /// Another returns { "result": {...} }
    /// Another returns just the raw object.
    /// The frontend has to handle 6 different shapes.
    ///
    /// With ApiResponse<T>, every single endpoint in every
    /// service always returns the same shape:
    /// {
    ///     "success": true,
    ///     "data": { ... },
    ///     "error": null,
    ///     "traceId": "4bf92f35..."
    /// }
    ///
    /// T = generic type parameter.
    /// ApiResponse<Order> wraps an Order object.
    /// ApiResponse<List<Product>> wraps a list of products.
    /// ApiResponse<string> wraps a simple string message.
    /// </summary>
    public record ApiResponse<T>
    {
        /// <summary>
        /// Did the operation succeed or fail?
        /// true  = everything worked, Data has the result.
        /// false = something went wrong, Error has the reason.
        ///
        /// The frontend always checks this first before reading Data.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// The actual data being returned.
        /// Only populated when Success = true.
        ///
        /// T? means it can be null.
        /// When Success = false, Data will be null.
        /// When Success = true, Data will have the result.
        /// </summary>
        public T? Data { get; init; }

        /// <summary>
        /// Human-readable error message.
        /// Only populated when Success = false.
        ///
        /// Examples:
        ///   "Order not found"
        ///   "Insufficient stock"
        ///   "Payment declined"
        ///
        /// string? means it can be null.
        /// When Success = true, Error will be null.
        /// </summary>
        public string? Error { get; init; }

        /// <summary>
        /// The distributed trace ID for this request.
        /// Copied from the X-Trace-Id header injected by the gateway.
        ///
        /// WHY INCLUDE THIS IN RESPONSE:
        /// When something goes wrong, the frontend can show:
        /// "Something went wrong. Reference: 4bf92f35"
        ///
        /// The user gives that ID to support.
        /// Support searches Seq logs by that TraceId and finds
        /// EXACTLY what happened across all services in seconds.
        ///
        /// Without this, the user says "it didn't work"
        /// and you have no starting point to investigate.
        /// </summary>
        public string TraceId { get; init; } = string.Empty;

        /// <summary>
        /// Creates a SUCCESS response with data.
        ///
        /// Usage in a controller:
        ///   return Ok(ApiResponse<Order>.Ok(order, traceId));
        ///
        /// This is a static factory method — it creates
        /// the object for you with the right fields set.
        /// You never manually set Success = true yourself.
        /// </summary>
        public static ApiResponse<T> Ok(T data, string traceId = "") =>
            new() { Success = true, Data = data, TraceId = traceId };

        /// <summary>
        /// Creates a FAILURE response with an error message.
        ///
        /// Usage in a controller:
        ///   return BadRequest(ApiResponse<Order>.Fail("Order not found", traceId));
        ///
        /// Notice Data is not set — it defaults to null.
        /// Only the error message and traceId are populated.
        /// </summary>
        public static ApiResponse<T> Fail(string error, string traceId = "") =>
            new() { Success = false, Error = error, TraceId = traceId };
    }
}
