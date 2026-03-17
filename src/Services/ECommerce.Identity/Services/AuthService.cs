using ECommerce.Identity.Data;
using ECommerce.Identity.DTOs;
using ECommerce.Identity.Interface;
using ECommerce.Identity.Models;
using ECommerce.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Identity.Services
{
    public class AuthService : IAuthService
    {
        private readonly IdentityDbContext _context;
        private readonly TokenService _tokenService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IdentityDbContext context, TokenService tokenService, ILogger<AuthService> logger)
        {
            _context = context;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLower();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

            var passwordValid = user != null && BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (user is null || !passwordValid)
            {
                _logger.LogWarning("Failed login attempt for email: {Email}", normalizedEmail);
                return ApiResponse<AuthResponse>.Fail("Invalid email or password.");
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt for inactive user: {Email}", normalizedEmail);
                return ApiResponse<AuthResponse>.Fail("Account is deactivated. Please contact support.");
            }

            var (accessToken, expiresAt) = _tokenService.GenerateToken(user);

            _logger.LogInformation("User logged in successfully: {Email} (ID: {UserId})", user.Email, user.Id);

            var response = new AuthResponse
            {
                AccessToken = accessToken,
                ExpiresAt = expiresAt,
                UserId = user.Id.ToString(),
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
            };

            return ApiResponse<AuthResponse>.Ok(response);

        }

        public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
        {
            var normalizedEmail = request.Email.Trim().ToLower();

            var emailExists = await _context.Users.AnyAsync(u => u.Email == normalizedEmail);

            if (emailExists)
            {
                _logger.LogWarning("Registration attempt with existing email: {Email}", normalizedEmail);
                return ApiResponse<AuthResponse>.Fail("Email is already registered.");
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 11);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                FullName = request.FullName.Trim(),
                PasswordHash = passwordHash,
                Role = "Customer",
                TenantId = "default",
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New user registered: {Email} (ID: {UserId})", user.Email, user.Id);

            var (accessToken, expiresAt) = _tokenService.GenerateToken(user);

            var response = new AuthResponse
            {
                AccessToken = accessToken,
                ExpiresAt = expiresAt,
                UserId = user.Id.ToString(),
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role
            };

            return ApiResponse<AuthResponse>.Ok(response);

        }
    }
}
