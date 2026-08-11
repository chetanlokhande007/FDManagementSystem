using FinTrustFDManager.DAL.Data;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinTrustFDManager.DAL.Repositories
{
    public class CurrencyRepository : ICurrencyRepository
    {
        private readonly ApplicationDbContext _context;

        public CurrencyRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Currency>> GetAllAsync()
        {
            return await _context.Currencies
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Currency?> GetByIdAsync(int id)
        {
            return await _context.Currencies
                .FirstOrDefaultAsync(x => x.CurrencyId == id);
        }

        public async Task<Currency?> GetByCodeAsync(string code)
        {
            return await _context.Currencies
                .FirstOrDefaultAsync(x => x.CurrencyCode == code);
        }

        public async Task<Currency> CreateAsync(Currency currency)
        {
            _context.Currencies.Add(currency);

            await _context.SaveChangesAsync();

            return currency;
        }

        public async Task<Currency> UpdateAsync(Currency currency)
        {
            _context.Currencies.Update(currency);

            await _context.SaveChangesAsync();

            return currency;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var currency = await _context.Currencies
                .FirstOrDefaultAsync(x => x.CurrencyId == id);

            if (currency == null)
            {
                return false;
            }

            _context.Currencies.Remove(currency);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
