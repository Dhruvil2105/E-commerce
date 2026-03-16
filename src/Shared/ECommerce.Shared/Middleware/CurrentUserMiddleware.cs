using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Shared.Middleware
{
    /// <summary>
    /// ASP.NET Core middleware that runs on EVERY incoming request.
    ///
    /// WHAT IT DOES:
    /// Reads the trusted headers that the API Gateway injected
    /// and populates the CurrentUser object for this request.
    ///
    /// MIDDLEWARE PIPELINE CONCEPT:
    /// Every HTTP request passes through a pipeline of middleware.
    /// Think of it like a series of checkpoints at an airport:
    ///
    ///   Request arrives
    ///       → Security check (Authentication middleware)
    ///       → Passport check (CurrentUserMiddleware) ← THIS FILE
    ///       → Customs (Authorization middleware)
    ///       → Final destination (Your Controller)
    ///
    /// Each middleware can:
    ///   1. Do something BEFORE passing to the next middleware
    ///   2. Call the next middleware (await _next(context))
    ///   3. Do something AFTER the next middleware returns
    ///
    /// HOW TO REGISTER:
    /// In each service's Program.cs:
    ///   app.UseCurrentUser();  ← calls UseMiddleware<CurrentUserMiddleware>()
    /// </summary>
    public class CurrentUserMiddleware
    {
        /// <summary>
        /// _next represents the next middleware in the pipeline.
        /// We must call it to pass the request forward.
        /// If we forget to call it, the request stops here
        /// and the controller never runs.
        /// </summary>
        private readonly RequestDelegate _next;

        /// <summary>
        /// Constructor — ASP.NET Core injects _next automatically.
        /// This is the DI system at work.
        /// </summary>
        public CurrentUserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// InvokeAsync runs on every single HTTP request.
        ///
        /// HttpContext = everything about the current request.
        ///   context.Request.Headers = all incoming headers
        ///   context.Response        = what we send back
        ///   context.RequestServices = the DI container for this request
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            // Get the scoped CurrentUser from the DI container.
            // "Scoped" means one instance per HTTP request.
            // This same instance is shared by all classes
            // that inject CurrentUser during this one request.
            var currentUser = context.RequestServices
                .GetRequiredService<CurrentUser>();

            // Read each header that the API Gateway injected.
            // ToString() converts StringValues to plain string.
            // If the header is missing, ToString() returns ""
            // which is safe — IsAuthenticated will return false.
            currentUser.UserId = context.Request.Headers["X-User-Id"].ToString();
            currentUser.Email = context.Request.Headers["X-User-Email"].ToString();
            currentUser.TenantId = context.Request.Headers["X-Tenant-Id"].ToString();
            currentUser.TraceId = context.Request.Headers["X-Trace-Id"].ToString();

            // Roles come as a comma-separated string: "Admin,Customer"
            // We split it into an array: ["Admin", "Customer"]
            // RemoveEmptyEntries handles edge case of trailing commas.
            var rolesHeader = context.Request.Headers["X-Roles"].ToString();
            currentUser.Roles = string.IsNullOrEmpty(rolesHeader)
                ? []
                : rolesHeader.Split(',', StringSplitOptions.RemoveEmptyEntries);

            // Pass the request to the next middleware in the pipeline.
            // Everything ABOVE this line runs BEFORE the controller.
            // Everything you would write BELOW this line would run
            // AFTER the controller returns — useful for response logging.
            await _next(context);
        }
    }

    /// <summary>
    /// Extension methods so services can register this middleware
    /// with clean, readable one-liners in Program.cs
    ///
    /// EXTENSION METHODS:
    /// These add new methods to existing classes (IServiceCollection,
    /// IApplicationBuilder) without modifying those classes.
    ///
    /// Instead of writing:
    ///   services.AddScoped<CurrentUser>();
    ///   app.UseMiddleware<CurrentUserMiddleware>();
    ///
    /// Any service just writes:
    ///   services.AddCurrentUser();
    ///   app.UseCurrentUser();
    ///
    /// This is the same pattern Microsoft uses — AddAuthentication(),
    /// UseRouting(), UseAuthorization() are all extension methods.
    /// </summary>
    public static class CurrentUserExtensions
    {
        /// <summary>
        /// Registers CurrentUser into the DI container as Scoped.
        ///
        /// SCOPED means: one new instance per HTTP request.
        /// All classes injecting CurrentUser during the same request
        /// get the exact same instance — already populated with user data.
        ///
        /// Call this in each service's Program.cs:
        ///   builder.Services.AddCurrentUser();
        /// </summary>
        public static IServiceCollection AddCurrentUser(
            this IServiceCollection services)
        {
            services.AddScoped<CurrentUser>();
            return services;
        }

        /// <summary>
        /// Adds CurrentUserMiddleware to the request pipeline.
        ///
        /// Call this in each service's Program.cs:
        ///   app.UseCurrentUser();
        ///
        /// ORDER MATTERS in the pipeline.
        /// This must come AFTER app.UseRouting()
        /// but BEFORE app.MapControllers()
        /// so controllers already have CurrentUser populated
        /// when they start executing.
        /// </summary>
        public static IApplicationBuilder UseCurrentUser(
            this IApplicationBuilder app)
        {
            app.UseMiddleware<CurrentUserMiddleware>();
            return app;
        }
    }
}
