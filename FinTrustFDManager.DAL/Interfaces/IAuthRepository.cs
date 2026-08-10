using FinTrustFDManager.Model.Entities;

namespace FinTrustFDManager.DAL.Interfaces
{
    public interface IAuthRepository
    {
        Task<bool> Register(User user);
    }
}
