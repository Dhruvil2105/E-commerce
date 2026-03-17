namespace ECommerce.Identity.Models
{
    public class User
    {
        /// <summary>
        /// Primary key in the database.
        /// Guid instead of int because:
        ///   - No sequential guessing (security)
        ///   - Works across distributed systems
        ///   - No need to ask DB for next ID value
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// User's email address.
        /// Used as the login username.
        /// Must be unique — two users cannot share the same email.
        /// Stored in lowercase to avoid case-sensitivity issues.
        /// Example: "ravi@example.com"
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// BCrypt hashed password — NEVER the plain text password.
        ///
        /// HOW BCRYPT WORKS:
        /// User types "mypassword123"
        ///   → BCrypt hashes it → "$2a$11$xyz..."
        ///   → We store "$2a$11$xyz..." in the database
        ///
        /// On login:
        /// User types "mypassword123" again
        ///   → BCrypt.Verify("mypassword123", "$2a$11$xyz...") → true/false
        ///   → We never decrypt — we just verify
        ///
        /// Even if the database is stolen, passwords cannot be recovered.
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// User's display name.
        /// Included in the JWT token so other services
        /// can display the user's name without calling
        /// Identity Service.
        /// Example: "Ravi Sharma"
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// User's role in the system.
        /// Included in JWT token → gateway reads it →
        /// injects as X-Roles header → services enforce access.
        ///
        /// Possible values:
        ///   "Customer" → can browse and place orders
        ///   "Admin"    → can manage products and view all orders
        ///
        /// Default is "Customer" — most users are customers.
        /// </summary>
        public string Role { get; set; } = "Customer";

        /// <summary>
        /// Multi-tenant support — which company/organisation
        /// this user belongs to.
        ///
        /// Included in JWT → gateway injects as X-Tenant-Id →
        /// every service filters all DB queries by this value.
        ///
        /// Default "default" means single-tenant mode.
        /// In a real multi-tenant system this would be
        /// set during registration based on which company
        /// the user signed up for.
        /// </summary>
        public string TenantId { get; set; } = "default";

        /// <summary>
        /// When this account was created.
        /// Always UTC — never local time.
        /// Useful for audit trails and user analytics.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Is this account active?
        /// false = account is disabled (banned or deactivated).
        /// Inactive users cannot login even with correct password.
        /// Soft delete — we never delete user records.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
