using ECommerce.Identity.DTOs;
using ECommerce.Shared.DTOs;

namespace ECommerce.Identity.Interface
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request);

        Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
    }
}
