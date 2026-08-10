using FinTrustFDManager.DAL.Data;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinTrustFDManager.DAL.Repositories
{
    public class CounterPartyRepository : ICounterPartyRepository
    {
        private readonly ApplicationDbContext _context;

        public CounterPartyRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CounterParty>> GetAllAsync()
        {
            return await _context.CounterParties
                .Include(x => x.Country)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<CounterParty?> GetByIdAsync(int id)
        {
            return await _context.CounterParties
                .Include(x => x.Country)
                .FirstOrDefaultAsync(x => x.CounterPartyId == id);
        }

        public async Task<CounterParty?> GetByCodeAsync(string code)
        {
            return await _context.CounterParties
                .FirstOrDefaultAsync(
                    x => x.CounterPartyCode == code);
        }

        public async Task<CounterParty> CreateAsync(
            CounterParty counterParty)
        {
            _context.CounterParties.Add(counterParty);

            await _context.SaveChangesAsync();

            return counterParty;
        }

        public async Task<CounterParty> UpdateAsync(
            CounterParty counterParty)
        {
            _context.CounterParties.Update(counterParty);

            await _context.SaveChangesAsync();

            return counterParty;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var counterParty = await _context.CounterParties
                .FirstOrDefaultAsync(
                    x => x.CounterPartyId == id);

            if (counterParty == null)
            {
                return false;
            }

            _context.CounterParties.Remove(counterParty);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
