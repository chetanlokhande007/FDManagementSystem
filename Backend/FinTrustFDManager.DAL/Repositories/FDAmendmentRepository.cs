using FinTrustFDManager.DAL.Data;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinTrustFDManager.DAL.Repositories
{
    public class FDAmendmentRepository : IFDAmendmentRepository
    {
        private readonly ApplicationDbContext _context;

        public FDAmendmentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FDAmendment?> GetByIdAsync(long amendmentId)
        {
            return await _context.FDAmendments
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AmendmentId == amendmentId);
        }

        public async Task<IEnumerable<FDAmendment>> GetByFdIdAsync(long fdId)
        {
            return await _context.FDAmendments
                .AsNoTracking()
                .Where(x => x.FdId == fdId)
                .OrderByDescending(x => x.RequestedDate)
                .ToListAsync();
        }

        public async Task<FDAmendment?> GetPendingByFdIdAsync(long fdId)
        {
            return await _context.FDAmendments
                .FirstOrDefaultAsync(x => x.FdId == fdId && x.Status == "PENDING_APPROVAL");
        }

        public async Task<FDAmendment> AddAsync(FDAmendment amendment)
        {
            _context.FDAmendments.Add(amendment);
            await _context.SaveChangesAsync();
            return amendment;
        }

        public async Task<FDAmendment?> UpdateAsync(FDAmendment amendment)
        {
            var existing = await _context.FDAmendments
                .FirstOrDefaultAsync(x => x.AmendmentId == amendment.AmendmentId);

            if (existing == null) return null;

            existing.Status = amendment.Status;
            existing.ApprovedBy = amendment.ApprovedBy;
            existing.ApprovedDate = amendment.ApprovedDate;
            existing.RejectedBy = amendment.RejectedBy;
            existing.RejectedDate = amendment.RejectedDate;
            existing.ApprovalComments = amendment.ApprovalComments;
            existing.RejectionComments = amendment.RejectionComments;

            await _context.SaveChangesAsync();
            return existing;
        }
    }
}
