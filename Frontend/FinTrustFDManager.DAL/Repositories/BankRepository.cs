using FinTrustFDManager.DAL.Data;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinTrustFDManager.DAL.Repositories
{
    public class BankRepository : IBankRepository
    {
        private readonly ApplicationDbContext _context;

        public BankRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Bank>> GetAllAsync()
        {
            return await _context.Banks
                .Include(x => x.Country)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Bank?> GetByIdAsync(int id)
        {
            return await _context.Banks
                .Include(x => x.Country)
                .FirstOrDefaultAsync(x => x.BankId == id);
        }

        public async Task<Bank?> GetByCodeAsync(string code)
        {
            return await _context.Banks
                .FirstOrDefaultAsync(x => x.BankCode == code);
        }

        public async Task<Bank> CreateAsync(Bank bank)
        {
            _context.Banks.Add(bank);

            await _context.SaveChangesAsync();

            return bank;
        }

        public async Task<Bank> UpdateAsync(Bank bank)
        {
            _context.Banks.Update(bank);

            await _context.SaveChangesAsync();

            return bank;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var bank = await _context.Banks
                .FirstOrDefaultAsync(x => x.BankId == id);

            if (bank == null)
            {
                return false;
            }

            _context.Banks.Remove(bank);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
