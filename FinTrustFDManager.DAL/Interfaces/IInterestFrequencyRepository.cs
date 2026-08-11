using FinTrustFDManager.Model.Entities.CoreData;

namespace FinTrustFDManager.DAL.Interfaces
{
    public interface IInterestFrequencyRepository
    {
        Task<List<InterestFrequency>> GetAllAsync();
        Task<InterestFrequency?> GetByIdAsync(int id);
        Task<InterestFrequency> CreateAsync(InterestFrequency entity);
        Task<InterestFrequency> UpdateAsync(InterestFrequency entity);
        Task<bool> DeleteAsync(int id);
    }
}
