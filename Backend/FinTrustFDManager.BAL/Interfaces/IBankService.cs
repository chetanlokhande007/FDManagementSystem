using FinTrustFDManager.Model.DTOs.Bank;

namespace FinTrustFDManager.BAL.Interfaces
{
    public interface IBankService
    {
        Task<List<BankDto>> GetAllAsync();

        Task<BankDto?> GetByIdAsync(int id);

        Task<BankDto> CreateAsync(CreateBankDto dto);

        Task<BankDto?> UpdateAsync(
            int id,
            UpdateBankDto dto);

        Task<bool> DeleteAsync(int id);
    }
}
