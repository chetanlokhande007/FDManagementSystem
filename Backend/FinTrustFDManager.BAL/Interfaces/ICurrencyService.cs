using FinTrustFDManager.Model.DTOs.Currency;

namespace FinTrustFDManager.BAL.Interfaces
{
    public interface ICurrencyService
    {
        Task<List<CurrencyDto>> GetAllAsync();

        Task<CurrencyDto?> GetByIdAsync(int id);

        Task<CurrencyDto> CreateAsync(CreateCurrencyDto dto);

        Task<CurrencyDto?> UpdateAsync(
            int id,
            UpdateCurrencyDto dto);

        Task<bool> DeleteAsync(int id);
    }
}
