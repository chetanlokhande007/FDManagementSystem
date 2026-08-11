using FinTrustFDManager.Model.Entities.MasterData;

namespace FinTrustFDManager.DAL.Interfaces
{
    public interface IBankAccountRepository
    {
        Task<List<BankAccount>> GetAllAsync();

        Task<BankAccount?> GetByIdAsync(int id);

        Task<BankAccount?> GetByAccountNumberAsync(
            string accountNumber);

        Task<BankAccount> CreateAsync(
            BankAccount bankAccount);

        Task<BankAccount> UpdateAsync(
            BankAccount bankAccount);

        Task<bool> DeleteAsync(int id);
    }
}
