using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Shared.Middleware;

/// <summary>
/// Restricts endpoint access to any authenticated user.
///
/// HOW IT WORKS:
/// Runs BEFORE the controller action executes.
/// Reads CurrentUser from DI — already populated by
/// CurrentUserMiddleware which ran earlier in the pipeline.
///
/// If user is not authenticated → returns 401 immediately.
/// Controller method never runs.
///
/// USAGE:
///   [RequireAuth]
///   [HttpGet("my-orders")]
///   public async Task<IActionResult> GetMyOrders() { }
///
/// DIFFERENCE FROM [AdminOnly]:
///   [RequireAuth] = any logged in user can access
///   [AdminOnly]   = only Admin role can access
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireAuthAttribute : Attribute, IActionFilter
{
    /// <summary>
    /// Runs BEFORE the controller action.
    /// Checks if the user is authenticated.
    /// </summary>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // Get CurrentUser from DI container.
        // Already populated by CurrentUserMiddleware.
        var currentUser = context.HttpContext.RequestServices
            .GetRequiredService<CurrentUser>();

        // Not authenticated → 401 Unauthorized
        if (!currentUser.IsAuthenticated)
        {
            context.Result = new ObjectResult(new
            {
                success = false,
                error = "Authentication required. Please login first.",
                data = (object?)null,
                traceId = currentUser.TraceId
            })
            {
                StatusCode = 401
            };
        }
    }

    /// <summary>
    /// Runs AFTER the controller action.
    /// Nothing to do here.
    /// </summary>
    public void OnActionExecuted(ActionExecutedContext context) { }
}