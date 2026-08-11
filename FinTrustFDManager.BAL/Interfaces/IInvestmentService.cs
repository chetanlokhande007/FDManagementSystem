using FinTrustFDManager.Model.DTOs.Investment;

namespace FinTrustFDManager.BAL.Interfaces
{
    public interface IInvestmentService
    {
        Task<List<InvestmentDto>> GetAllAsync();
        Task<InvestmentDto?> GetByIdAsync(int id);
        Task<InvestmentDto> CreateAsync(CreateInvestmentDto dto);
        Task<InvestmentDto?> UpdateAsync(int id, UpdateInvestmentDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
