using FinTrustFDManager.Model.DTOs.BankAccount;

namespace FinTrustFDManager.BAL.Interfaces
{
    public interface IBankAccountService
    {
        Task<List<BankAccountDto>> GetAllAsync();

        Task<BankAccountDto?> GetByIdAsync(int id);

        Task<BankAccountDto> CreateAsync(
            CreateBankAccountDto dto);

        Task<BankAccountDto?> UpdateAsync(
            int id,
            UpdateBankAccountDto dto);

        Task<bool> DeleteAsync(int id);
    }
}
