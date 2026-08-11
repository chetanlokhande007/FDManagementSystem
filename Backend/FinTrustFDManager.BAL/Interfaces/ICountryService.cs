using FinTrustFDManager.Model.DTOs.Country;

namespace FinTrustFDManager.BAL.Interfaces
{
    public interface ICountryService
    {
        Task<List<CountryDto>> GetAllAsync();

        Task<CountryDto?> GetByIdAsync(int id);

        Task<CountryDto> CreateAsync(CreateCountryDto dto);

        Task<CountryDto?> UpdateAsync(
            int id,
            UpdateCountryDto dto);

        Task<bool> DeleteAsync(int id);
    }
}
