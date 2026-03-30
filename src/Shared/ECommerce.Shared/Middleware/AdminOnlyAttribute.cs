using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Shared.Middleware;

/// <summary>
/// Restricts endpoint access to Admin users only.
///
/// HOW IT WORKS:
/// Runs BEFORE the controller action executes.
/// Reads CurrentUser from DI — already populated by
/// CurrentUserMiddleware which ran earlier in the pipeline.
///
/// Not authenticated → 401 Unauthorized
/// Authenticated but not Admin → 403 Forbidden
/// Admin → action executes normally
///
/// USAGE:
///   [AdminOnly]
///   [HttpPost]
///   public async Task<IActionResult> Create(...) { }
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class AdminOnlyAttribute : Attribute, IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var currentUser = context.HttpContext.RequestServices
            .GetRequiredService<CurrentUser>();

        // Not logged in → 401
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
            return;
        }

        // Logged in but not Admin → 403
        if (!currentUser.IsAdmin)
        {
            context.Result = new ObjectResult(new
            {
                success = false,
                error = "Admin access required.",
                data = (object?)null,
                traceId = currentUser.TraceId
            })
            {
                StatusCode = 403
            };
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}