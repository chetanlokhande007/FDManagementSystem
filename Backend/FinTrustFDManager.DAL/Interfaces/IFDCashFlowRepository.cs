using FinTrustFDManager.Model.Entities.Investment;

namespace FinTrustFDManager.DAL.Interfaces
{
    public interface IFDCashFlowRepository
    {
        Task<IEnumerable<FDCashFlow>> GetAllAsync();

        Task<FDCashFlow?> GetByIdAsync(long id);

        Task<IEnumerable<FDCashFlow>> GetByFdIdAsync(long fdId);

        Task<FDCashFlow> CreateAsync(FDCashFlow cashFlow);

        Task AddRangeAsync(IEnumerable<FDCashFlow> models);

        Task<FDCashFlow?> UpdateAsync(FDCashFlow cashFlow);

        Task UpdateRangeAsync(IEnumerable<FDCashFlow> cashFlows);

        Task<bool> DeleteAsync(long id);

        Task DeleteRangeAsync(IEnumerable<FDCashFlow> cashFlows);
    }
}
