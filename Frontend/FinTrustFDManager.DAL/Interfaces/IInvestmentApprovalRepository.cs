using FinTrustFDManager.Model.Entities.CoreData;

namespace FinTrustFDManager.DAL.Interfaces
{
    public interface IInvestmentApprovalRepository
    {
        Task<List<InvestmentApproval>> GetAllAsync();
        Task<List<InvestmentApproval>> GetByInvestmentIdAsync(int investmentId);
        Task<InvestmentApproval?> GetByIdAsync(int id);
        Task<InvestmentApproval> CreateAsync(InvestmentApproval approval);
        Task<InvestmentApproval> UpdateAsync(InvestmentApproval approval);
        Task<bool> DeleteAsync(int id);
    }
}
