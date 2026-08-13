using FinTrustFDManager.Model.Entities.Investment;

namespace FinTrustFDManager.DAL.Interfaces
{
    public interface IFDCashFlowRepository
    {
        Task<IEnumerable<FDCashFlow>> GetAllAsync();

        Task<FDCashFlow?> GetByIdAsync(long id);

        Task<FDCashFlow> CreateAsync(FDCashFlow cashFlow);

        Task<FDCashFlow?> UpdateAsync(FDCashFlow cashFlow);

        Task<bool> DeleteAsync(long id);
    }
}
