using FinTrustFDManager.Model.DTOs;

namespace FinTrustFDManager.BAL.Interfaces
{
    public interface IAuthService
    {
        Task<bool> Register(RegisterDto dto);
    }
}
