using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Shared.Middleware
{
    /// <summary>
    /// Represents the currently authenticated user making the request.
    ///
    /// HOW IT WORKS:
    /// The API Gateway validates the JWT token.
    /// After validation it STRIPS the JWT and injects these HTTP headers:
    ///   X-User-Id    → the user's unique ID
    ///   X-User-Email → the user's email address
    ///   X-Roles      → comma separated roles e.g. "Admin,Customer"
    ///   X-Tenant-Id  → which tenant/company this user belongs to
    ///   X-Trace-Id   → distributed trace ID for this request
    ///
    /// The CurrentUserMiddleware (next file) reads those headers
    /// and populates this class automatically on every request.
    ///
    /// Services never validate JWTs themselves.
    /// They just inject CurrentUser and read from it.
    ///
    /// USAGE IN A CONTROLLER:
    ///   public class OrderController : ControllerBase
    ///   {
    ///       private readonly CurrentUser _currentUser;
    ///
    ///       public OrderController(CurrentUser currentUser)
    ///       {
    ///           _currentUser = currentUser;
    ///       }
    ///
    ///       public IActionResult PlaceOrder()
    ///       {
    ///           var userId = _currentUser.UserId;
    ///           // use it directly — no JWT parsing needed
    ///       }
    ///   }
    /// </summary>
    public class CurrentUser
    {
        // <summary>
        /// Unique identifier of the logged in user.
        /// Comes from X-User-Id header injected by gateway.
        ///
        /// Every database query that belongs to a user
        /// must filter by this ID.
        ///
        /// Example: "usr_9f3a12"
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Email address of the logged in user.
        /// Comes from X-User-Email header injected by gateway.
        ///
        /// Useful for sending emails or displaying user info
        /// without making a separate call to Identity Service.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Roles assigned to this user.
        /// Comes from X-Roles header injected by gateway.
        ///
        /// The gateway sends roles as a comma-separated string:
        /// "Admin,Customer"
        ///
        /// The middleware splits it into an array:
        /// ["Admin", "Customer"]
        ///
        /// Usage:
        ///   if (_currentUser.IsAdmin) { ... }
        ///   if (_currentUser.HasRole("Manager")) { ... }
        /// </summary>
        public string[] Roles { get; set; } = [];

        /// <summary>
        /// Which tenant (company/organisation) this user belongs to.
        /// Comes from X-Tenant-Id header injected by gateway.
        ///
        /// CRITICAL: Every single database query MUST include
        /// a WHERE TenantId = _currentUser.TenantId filter.
        ///
        /// Forgetting this even once means one company can
        /// accidentally see another company's data.
        ///
        /// Example: "tenant_acme"
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// The distributed trace ID for this request.
        /// Comes from X-Trace-Id header injected by gateway.
        ///
        /// Every log entry and every RabbitMQ message
        /// published during this request should include this ID.
        ///
        /// This is what links all logs across all services
        /// for one specific user request.
        /// </summary>
        public string TraceId { get; set; } = string.Empty;

        /// <summary>
        /// Quick check — is there an authenticated user on this request?
        ///
        /// Returns true  if UserId is not empty (user is logged in).
        /// Returns false if UserId is empty  (anonymous request).
        ///
        /// This is a computed property — no need to store a separate boolean.
        /// It checks UserId automatically every time you access it.
        /// </summary>
        public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);

        /// <summary>
        /// Quick check — does this user have the Admin role?
        ///
        /// Usage:
        ///   if (!_currentUser.IsAdmin)
        ///       return Forbid();
        ///
        /// Computed property — checks the Roles array every time.
        /// </summary>
        public bool IsAdmin => Roles.Contains("Admin");

        /// <summary>
        /// Check if this user has a specific role.
        ///
        /// More flexible than IsAdmin — works for any role name.
        ///
        /// Usage:
        ///   if (_currentUser.HasRole("Manager")) { ... }
        ///   if (_currentUser.HasRole("Customer")) { ... }
        /// </summary>
        public bool HasRole(string role) => Roles.Contains(role);

    }
}
