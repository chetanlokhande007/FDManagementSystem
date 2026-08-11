using FinTrustFDManager.DAL.Data;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities.CoreData;
using Microsoft.EntityFrameworkCore;

namespace FinTrustFDManager.DAL.Repositories
{
    public class InvestmentApprovalRepository : IInvestmentApprovalRepository
    {
        private readonly ApplicationDbContext _context;

        public InvestmentApprovalRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<InvestmentApproval>> GetAllAsync()
        {
            return await _context.InvestmentApprovals
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<InvestmentApproval>> GetByInvestmentIdAsync(int investmentId)
        {
            return await _context.InvestmentApprovals
                .Where(x => x.InvestmentId == investmentId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<InvestmentApproval?> GetByIdAsync(int id)
        {
            return await _context.InvestmentApprovals
                .FirstOrDefaultAsync(x => x.InvestmentApprovalId == id);
        }

        public async Task<InvestmentApproval> CreateAsync(InvestmentApproval approval)
        {
            _context.InvestmentApprovals.Add(approval);
            await _context.SaveChangesAsync();
            return approval;
        }

        public async Task<InvestmentApproval> UpdateAsync(InvestmentApproval approval)
        {
            _context.InvestmentApprovals.Update(approval);
            await _context.SaveChangesAsync();
            return approval;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var approval = await _context.InvestmentApprovals
                .FirstOrDefaultAsync(x => x.InvestmentApprovalId == id);

            if (approval == null)
                return false;

            _context.InvestmentApprovals.Remove(approval);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
