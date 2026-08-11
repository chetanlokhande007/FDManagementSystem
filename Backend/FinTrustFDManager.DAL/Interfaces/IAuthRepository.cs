using FinTrustFDManager.Model.Entities;

namespace FinTrustFDManager.DAL.Interfaces
{
    public interface IAuthRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<Role?> GetRoleByIdAsync(int roleId);
        Task AddUserAsync(User user);
        Task<bool> SaveChangesAsync();
    }
}
