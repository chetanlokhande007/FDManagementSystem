using FinTrustFDManager.Model.Entities;

namespace FinTrustFDManager.DAL.Interfaces
{
    public interface IBankRepository
    {
        Task<List<Bank>> GetAllAsync();

        Task<Bank?> GetByIdAsync(int id);

        Task<Bank?> GetByCodeAsync(string code);

        Task<Bank> CreateAsync(Bank bank);

        Task<Bank> UpdateAsync(Bank bank);

        Task<bool> DeleteAsync(int id);
    }
}
