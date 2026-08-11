using FinTrustFDManager.DAL.Data;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities.CoreData;
using Microsoft.EntityFrameworkCore;

namespace FinTrustFDManager.DAL.Repositories
{
    public class CashFlowRepository : ICashFlowRepository
    {
        private readonly ApplicationDbContext _context;

        public CashFlowRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CashFlow>> GetAllAsync()
        {
            return await _context.CashFlows
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<CashFlow>> GetByInvestmentIdAsync(int investmentId)
        {
            return await _context.CashFlows
                .Where(x => x.InvestmentId == investmentId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<CashFlow?> GetByIdAsync(int id)
        {
            return await _context.CashFlows
                .FirstOrDefaultAsync(x => x.CashFlowId == id);
        }

        public async Task<CashFlow> CreateAsync(CashFlow cashFlow)
        {
            _context.CashFlows.Add(cashFlow);
            await _context.SaveChangesAsync();
            return cashFlow;
        }

        public async Task<CashFlow> UpdateAsync(CashFlow cashFlow)
        {
            _context.CashFlows.Update(cashFlow);
            await _context.SaveChangesAsync();
            return cashFlow;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var cashFlow = await _context.CashFlows
                .FirstOrDefaultAsync(x => x.CashFlowId == id);

            if (cashFlow == null)
                return false;

            _context.CashFlows.Remove(cashFlow);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
