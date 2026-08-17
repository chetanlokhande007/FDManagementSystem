using FinTrustFDManager.DAL.Data;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities.Investment;
using Microsoft.EntityFrameworkCore;

namespace FinTrustFDManager.DAL.Repositories
{
    public class FDIdentificationRepository : IFDIdentificationRepository
    {
        private readonly ApplicationDbContext _context;

        public FDIdentificationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<FDIdentification>> GetAllAsync()
        {
            return await _context.FDIdentifications
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<FDIdentification?> GetByIdAsync(long id)
        {
            return await _context.FDIdentifications
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FdId == id);
        }

        public async Task<FDIdentification> AddAsync(FDIdentification model)
        {
            _context.FDIdentifications.Add(model);

            await _context.SaveChangesAsync();

            return model;
        }
        public async Task<FDIdentification?> GetLastAsync()
        {
            return await _context.FDIdentifications
                .AsNoTracking()
                .OrderByDescending(x => x.FdId)
                .FirstOrDefaultAsync();
        }
        public async Task<FDIdentification?> UpdateAsync(
            FDIdentification model)
        {
            var existing = await _context.FDIdentifications
                .FirstOrDefaultAsync(x => x.FdId == model.FdId);

            if (existing == null)
                return null;

            existing.FdReferenceNo = model.FdReferenceNo;
            existing.EntityId = model.EntityId;
            existing.CounterpartyId = model.CounterpartyId;
            existing.CurrencyCode = model.CurrencyCode;
            existing.PrincipalAmount = model.PrincipalAmount;
            existing.StartDate = model.StartDate;
            existing.EndDate = model.EndDate;
            existing.SettlementDate = model.SettlementDate;
            existing.BankAccountId = model.BankAccountId;
            existing.Status = model.Status;
            existing.Remarks = model.Remarks;
            existing.ModifiedBy = model.ModifiedBy;
            existing.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return existing;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var existing = await _context.FDIdentifications
                .FirstOrDefaultAsync(x => x.FdId == id);

            if (existing == null)
                return false;

            _context.FDIdentifications.Remove(existing);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}