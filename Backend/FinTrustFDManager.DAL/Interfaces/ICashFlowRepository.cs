using FinTrustFDManager.Model.Entities.CoreData;

namespace FinTrustFDManager.DAL.Interfaces
{
    public interface ICashFlowRepository
    {
        Task<List<CashFlow>> GetAllAsync();
        Task<List<CashFlow>> GetByInvestmentIdAsync(int investmentId);
        Task<CashFlow?> GetByIdAsync(int id);
        Task<CashFlow> CreateAsync(CashFlow cashFlow);
        Task<CashFlow> UpdateAsync(CashFlow cashFlow);
        Task<bool> DeleteAsync(int id);
    }
}
