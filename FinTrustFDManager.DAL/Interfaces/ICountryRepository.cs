using FinTrustFDManager.Model.Entities;

namespace FinTrustFDManager.DAL.Interfaces
{
    public interface ICountryRepository
    {
        Task<List<Country>> GetAllAsync();

        Task<Country?> GetByIdAsync(int id);

        Task<Country?> GetByCodeAsync(string code);

        Task<Country> CreateAsync(Country country);

        Task<Country> UpdateAsync(Country country);

        Task<bool> DeleteAsync(int id);
    }
}
