using FinTrustFDManager.DAL.Data;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities.Investment;
using Microsoft.EntityFrameworkCore;

namespace FinTrustFDManager.DAL.Repositories
{
    public class FDInterestRepository : IFDInterestRepository
    {
        private readonly ApplicationDbContext _context;

        public FDInterestRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<FDInterest>> GetAllAsync()
        {
            return await _context.FDInterests
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<FDInterest?> GetByIdAsync(long id)
        {
            return await _context.FDInterests
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FdInterestId == id);
        }

        public async Task<FDInterest?> GetByFdIdAsync(long fdId)
        {
            return await _context.FDInterests
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FdId == fdId);
        }

        public async Task<FDInterest> AddAsync(FDInterest model)
        {
            _context.FDInterests.Add(model);

            await _context.SaveChangesAsync();

            return model;
        }

        public async Task<FDInterest?> UpdateAsync(FDInterest model)
        {
            var existing = await _context.FDInterests
                .FirstOrDefaultAsync(x => x.FdInterestId == model.FdInterestId);

            if (existing == null)
                return null;

            existing.FdId = model.FdId;
            existing.InterestRateType = model.InterestRateType;
            existing.InterestRate = model.InterestRate;
            existing.BenchmarkName = model.BenchmarkName;
            existing.BenchmarkRate = model.BenchmarkRate;
            existing.Margin = model.Margin;
            existing.InterestFrequency = model.InterestFrequency;
            existing.CompoundingFrequency = model.CompoundingFrequency;
            existing.CalculationBasis = model.CalculationBasis;
            existing.PaymentConvention = model.PaymentConvention;

            await _context.SaveChangesAsync();

            return existing;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var existing = await _context.FDInterests
                .FirstOrDefaultAsync(x => x.FdInterestId == id);

            if (existing == null)
                return false;

            _context.FDInterests.Remove(existing);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}