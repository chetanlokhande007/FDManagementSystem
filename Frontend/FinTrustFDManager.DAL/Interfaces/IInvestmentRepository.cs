using FinTrustFDManager.Model.Entities.CoreData;

namespace FinTrustFDManager.DAL.Interfaces
{
    public interface IInvestmentRepository
    {
        Task<List<Investment>> GetAllAsync();
        Task<Investment?> GetByIdAsync(int id);
        Task<Investment?> GetByReferenceNoAsync(string referenceNo);
        Task<Investment> CreateAsync(Investment investment);
        Task<Investment> UpdateAsync(Investment investment);
        Task<bool> DeleteAsync(int id);
    }
}
