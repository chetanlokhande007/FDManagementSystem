using FinTrustFDManager.Model.DTOs.InvestmentApproval;

namespace FinTrustFDManager.BAL.Interfaces
{
    public interface IInvestmentApprovalService
    {
        Task<List<InvestmentApprovalDto>> GetAllAsync();
        Task<List<InvestmentApprovalDto>> GetByInvestmentIdAsync(int investmentId);
        Task<InvestmentApprovalDto?> GetByIdAsync(int id);
        Task<InvestmentApprovalDto> CreateAsync(CreateInvestmentApprovalDto dto);
        Task<InvestmentApprovalDto?> UpdateAsync(int id, UpdateInvestmentApprovalDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
