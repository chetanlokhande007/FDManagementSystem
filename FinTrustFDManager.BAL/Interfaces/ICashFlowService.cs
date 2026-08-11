using FinTrustFDManager.Model.DTOs.CashFlow;

namespace FinTrustFDManager.BAL.Interfaces
{
    public interface ICashFlowService
    {
        Task<List<CashFlowDto>> GetAllAsync();
        Task<List<CashFlowDto>> GetByInvestmentIdAsync(int investmentId);
        Task<CashFlowDto?> GetByIdAsync(int id);
        Task<CashFlowDto> CreateAsync(CreateCashFlowDto dto);
        Task<CashFlowDto?> UpdateAsync(int id, UpdateCashFlowDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
