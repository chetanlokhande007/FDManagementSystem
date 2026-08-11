using FinTrustFDManager.Model.DTOs.InterestFrequency;

namespace FinTrustFDManager.BAL.Interfaces
{
    public interface IInterestFrequencyService
    {
        Task<List<InterestFrequencyDto>> GetAllAsync();
        Task<InterestFrequencyDto?> GetByIdAsync(int id);
        Task<InterestFrequencyDto> CreateAsync(CreateInterestFrequencyDto dto);
        Task<InterestFrequencyDto?> UpdateAsync(int id, UpdateInterestFrequencyDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
