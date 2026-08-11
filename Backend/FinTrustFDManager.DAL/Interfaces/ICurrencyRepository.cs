using FinTrustFDManager.Model.Entities;

namespace FinTrustFDManager.DAL.Interfaces
{
    public interface ICurrencyRepository
    {
        Task<List<Currency>> GetAllAsync();

        Task<Currency?> GetByIdAsync(int id);

        Task<Currency?> GetByCodeAsync(string code);

        Task<Currency> CreateAsync(Currency currency);

        Task<Currency> UpdateAsync(Currency currency);

        Task<bool> DeleteAsync(int id);
    }
}
