using FinTrustFDManager.Model.Entities.CoreData;

namespace FinTrustFDManager.DAL.Interfaces
{
    public interface IDayCountConventionRepository
    {
        Task<List<DayCountConvention>> GetAllAsync();
        Task<DayCountConvention?> GetByIdAsync(int id);
        Task<DayCountConvention> CreateAsync(DayCountConvention entity);
        Task<DayCountConvention> UpdateAsync(DayCountConvention entity);
        Task<bool> DeleteAsync(int id);
    }
}
