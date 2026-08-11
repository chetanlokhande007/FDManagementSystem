using FinTrustFDManager.Model.DTOs.CounterParty;

namespace FinTrustFDManager.BAL.Interfaces
{
    public interface ICounterPartyService
    {
        Task<List<CounterPartyDto>> GetAllAsync();

        Task<CounterPartyDto?> GetByIdAsync(int id);

        Task<CounterPartyDto> CreateAsync(
            CreateCounterPartyDto dto);

        Task<CounterPartyDto?> UpdateAsync(
            int id,
            UpdateCounterPartyDto dto);

        Task<bool> DeleteAsync(int id);
    }
}
