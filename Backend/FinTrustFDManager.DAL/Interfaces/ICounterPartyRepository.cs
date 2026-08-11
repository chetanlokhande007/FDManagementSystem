using FinTrustFDManager.Model.Entities;

namespace FinTrustFDManager.DAL.Interfaces
{
    public interface ICounterPartyRepository
    {
        Task<List<CounterParty>> GetAllAsync();

        Task<CounterParty?> GetByIdAsync(int id);

        Task<CounterParty?> GetByCodeAsync(string code);

        Task<CounterParty> CreateAsync(CounterParty counterParty);

        Task<CounterParty> UpdateAsync(CounterParty counterParty);

        Task<bool> DeleteAsync(int id);
    }
}
