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
            existingCashFlow.CashFlowDate = cashFlow.CashFlowDate;
            existingCashFlow.CashFlowType = cashFlow.CashFlowType;
            existingCashFlow.Direction = cashFlow.Direction;
            existingCashFlow.PrincipalAmount = cashFlow.PrincipalAmount;
            existingCashFlow.GrossInterest = cashFlow.GrossInterest;
            existingCashFlow.TdsAmount = cashFlow.TdsAmount;
            existingCashFlow.NetInterest = cashFlow.NetInterest;
            existingCashFlow.TotalAmount = cashFlow.TotalAmount;
            existingCashFlow.CurrencyCode = cashFlow.CurrencyCode;
            existingCashFlow.Status = cashFlow.Status;
            existingCashFlow.ReferenceNo = cashFlow.ReferenceNo;

            await _context.SaveChangesAsync();

            return existingCashFlow;
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
    }
}
