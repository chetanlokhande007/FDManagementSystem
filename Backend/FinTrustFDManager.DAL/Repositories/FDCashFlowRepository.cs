using FinTrustFDManager.DAL.Data;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities.Investment;
using Microsoft.EntityFrameworkCore;

namespace FinTrustFDManager.DAL.Repositories
{
    public class FDCashFlowRepository : IFDCashFlowRepository
    {
        private readonly ApplicationDbContext _context;

        public FDCashFlowRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET ALL
        public async Task<IEnumerable<FDCashFlow>> GetAllAsync()
        {
            return await _context.FDCashFlows
                .AsNoTracking()
                .ToListAsync();
        }

        // GET BY ID
        public async Task<FDCashFlow?> GetByIdAsync(long id)
        {
            return await _context.FDCashFlows
                .FirstOrDefaultAsync(x => x.CashFlowId == id);
        }

        public async Task<IEnumerable<FDCashFlow>> GetByFdIdAsync(long fdId)
        {
            return await _context.FDCashFlows
                .AsNoTracking()
                .Where(x => x.FdId == fdId)
                .ToListAsync();
        }

        // CREATE
        public async Task<FDCashFlow> CreateAsync(FDCashFlow cashFlow)
        {
            _context.FDCashFlows.Add(cashFlow);

            await _context.SaveChangesAsync();

            return cashFlow;
        }

        public async Task AddRangeAsync(IEnumerable<FDCashFlow> models)
        {
            await _context.FDCashFlows.AddRangeAsync(models);
            await _context.SaveChangesAsync();
        }

        // UPDATE
        public async Task<FDCashFlow?> UpdateAsync(FDCashFlow cashFlow)
        {
            var existingCashFlow =
                await _context.FDCashFlows
                    .FirstOrDefaultAsync(x =>
                        x.CashFlowId == cashFlow.CashFlowId);

            if (existingCashFlow == null)
                return null;

            existingCashFlow.FdId = cashFlow.FdId;
            existingCashFlow.Event = cashFlow.Event;
            existingCashFlow.StartDate = cashFlow.StartDate;
            existingCashFlow.EndDate = cashFlow.EndDate;
            existingCashFlow.Days = cashFlow.Days;
            existingCashFlow.InterestRate = cashFlow.InterestRate;
            existingCashFlow.OpeningBalance = cashFlow.OpeningBalance;
            existingCashFlow.InterestAmount = cashFlow.InterestAmount;
            existingCashFlow.ClosingBalance = cashFlow.ClosingBalance;
            existingCashFlow.CashFlowAmount = cashFlow.CashFlowAmount;
            existingCashFlow.Direction = cashFlow.Direction;
            existingCashFlow.CurrencyCode = cashFlow.CurrencyCode;
            existingCashFlow.Status = cashFlow.Status;
            existingCashFlow.ReferenceNo = cashFlow.ReferenceNo;

            await _context.SaveChangesAsync();

            return existingCashFlow;
        }

        public async Task UpdateRangeAsync(IEnumerable<FDCashFlow> cashFlows)
        {
            _context.FDCashFlows.UpdateRange(cashFlows);
            await _context.SaveChangesAsync();
        }

        // DELETE
        public async Task<bool> DeleteAsync(long id)
        {
            var cashFlow =
                await _context.FDCashFlows
                    .FirstOrDefaultAsync(x =>
                        x.CashFlowId == id);

            if (cashFlow == null)
                return false;

            _context.FDCashFlows.Remove(cashFlow);

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task DeleteRangeAsync(IEnumerable<FDCashFlow> cashFlows)
        {
            var cashFlowList = cashFlows.ToList();
            var ids = cashFlowList.Select(c => c.CashFlowId).ToHashSet();

            // Detach any tracked FDCashFlow entities with matching keys
            // to avoid tracking conflicts when the same entity was loaded
            // in a prior operation within the same DbContext scope.
            foreach (var entry in _context.ChangeTracker.Entries<FDCashFlow>()
                .Where(e => ids.Contains(e.Entity.CashFlowId))
                .ToList())
            {
                entry.State = EntityState.Detached;
            }

            _context.FDCashFlows.RemoveRange(cashFlowList);
            await _context.SaveChangesAsync();
        }
    }
}
