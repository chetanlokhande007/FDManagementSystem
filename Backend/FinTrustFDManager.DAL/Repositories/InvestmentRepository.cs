using FinTrustFDManager.DAL.Data;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities.CoreData;
using Microsoft.EntityFrameworkCore;

namespace FinTrustFDManager.DAL.Repositories
{
    public class InvestmentRepository : IInvestmentRepository
    {
        private readonly ApplicationDbContext _context;

        public InvestmentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Investment>> GetAllAsync()
        {
            return await _context.Investments
                .Include(x => x.Entity)
                .Include(x => x.Country)
                .Include(x => x.Currency)


                .Include(x => x.InterestFrequency)
                .Include(x => x.DayCountConvention)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Investment?> GetByIdAsync(int id)
        {
            return await _context.Investments
                .Include(x => x.Entity)
                .Include(x => x.Country)
                .Include(x => x.Currency)


                .Include(x => x.InterestFrequency)
                .Include(x => x.DayCountConvention)
                .Include(x => x.CashFlows)
                .Include(x => x.Approvals)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Investment?> GetByReferenceNoAsync(string referenceNo)
        {
            return await _context.Investments
                .Include(x => x.Entity)
                .Include(x => x.Country)
                .Include(x => x.Currency)


                .Include(x => x.InterestFrequency)
                .Include(x => x.DayCountConvention)
                .FirstOrDefaultAsync(x => x.InvestmentReferenceNo == referenceNo);
        }

        public async Task<Investment> CreateAsync(Investment investment)
        {
            _context.Investments.Add(investment);
            await _context.SaveChangesAsync();
            return investment;
        }

        public async Task<Investment> UpdateAsync(Investment investment)
        {
            _context.Investments.Update(investment);
            await _context.SaveChangesAsync();
            return investment;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var investment = await _context.Investments
                .FirstOrDefaultAsync(x => x.Id == id);

            if (investment == null)
                return false;

            _context.Investments.Remove(investment);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
