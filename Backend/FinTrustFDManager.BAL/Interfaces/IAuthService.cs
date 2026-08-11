using FinTrustFDManager.Model.DTOs;
using FinTrustFDManager.BAL.DTOs.Auth;

namespace FinTrustFDManager.BAL.Interfaces
{
    public interface IAuthService
    {
        Task<bool> RegisterAsync(RegisterRequest request);
        Task<LoginResponse?> LoginAsync(LoginRequest request);
    }
}
