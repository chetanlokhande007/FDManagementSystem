using FinTrustFDManager.DAL.Data;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities.MasterData;
using Microsoft.EntityFrameworkCore;

namespace FinTrustFDManager.DAL.Repositories
{
    public class BankAccountRepository : IBankAccountRepository
    {
        private readonly ApplicationDbContext _context;

        public BankAccountRepository(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<BankAccount>> GetAllAsync()
        {
            return await _context.BankAccounts
                .Include(x => x.Bank)
                .Include(x => x.Currency)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<BankAccount?> GetByIdAsync(int id)
        {
            return await _context.BankAccounts
                .Include(x => x.Bank)
                .Include(x => x.Currency)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<BankAccount?> GetByAccountNumberAsync(
            string accountNumber)
        {
            return await _context.BankAccounts
                .FirstOrDefaultAsync(
                    x => x.AccountNumber == accountNumber);
        }

        public async Task<BankAccount> CreateAsync(
            BankAccount bankAccount)
        {
            _context.BankAccounts.Add(bankAccount);

            await _context.SaveChangesAsync();

            return bankAccount;
        }

        public async Task<BankAccount> UpdateAsync(
            BankAccount bankAccount)
        {
            _context.BankAccounts.Update(bankAccount);

            await _context.SaveChangesAsync();

            return bankAccount;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var bankAccount =
                await _context.BankAccounts
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (bankAccount == null)
            {
                return false;
            }

            _context.BankAccounts.Remove(bankAccount);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
