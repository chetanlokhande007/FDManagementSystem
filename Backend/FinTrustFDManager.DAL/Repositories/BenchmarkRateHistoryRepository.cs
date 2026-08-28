using FinTrustFDManager.DAL.Data;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities.MasterData;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinTrustFDManager.DAL.Repositories
{
    public class BenchmarkRateHistoryRepository : IBenchmarkRateHistoryRepository
    {
        private readonly ApplicationDbContext _context;

        public BenchmarkRateHistoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BenchmarkRateHistory>> GetAllAsync()
        {
            return await _context.BenchmarkRateHistories
                .AsNoTracking()
                .OrderByDescending(x => x.EffectiveFrom)
                .ToListAsync();
        }

        public async Task<IEnumerable<BenchmarkRateHistory>> GetByBenchmarkIdAsync(int benchmarkId)
        {
            return await _context.BenchmarkRateHistories
                .AsNoTracking()
                .Where(x => x.BenchmarkId == benchmarkId)
                .OrderByDescending(x => x.EffectiveFrom)
                .ToListAsync();
        }

        public async Task<BenchmarkRateHistory?> GetByBenchmarkIdAndDateAsync(int benchmarkId, DateTime asOfDate)
        {
            return await _context.BenchmarkRateHistories
                .AsNoTracking()
                .Where(x => x.BenchmarkId == benchmarkId
                    && x.EffectiveFrom <= asOfDate
                    && (x.EffectiveTo == null || x.EffectiveTo >= asOfDate))
                .OrderByDescending(x => x.EffectiveFrom)
                .FirstOrDefaultAsync();
        }

        public async Task<BenchmarkRateHistory> AddAsync(BenchmarkRateHistory model)
        {
            _context.BenchmarkRateHistories.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<BenchmarkRateHistory?> UpdateAsync(BenchmarkRateHistory model)
        {
            var existing = await _context.BenchmarkRateHistories
                .FirstOrDefaultAsync(x => x.BenchmarkRateHistoryId == model.BenchmarkRateHistoryId);

            if (existing == null)
                return null;

            existing.BenchmarkId = model.BenchmarkId;
            existing.Rate = model.Rate;
            existing.EffectiveFrom = model.EffectiveFrom;
            existing.EffectiveTo = model.EffectiveTo;
            existing.ModifiedDate = DateTime.UtcNow;
            existing.ModifiedBy = model.ModifiedBy;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var existing = await _context.BenchmarkRateHistories
                .FirstOrDefaultAsync(x => x.BenchmarkRateHistoryId == id);

            if (existing == null)
                return false;

            _context.BenchmarkRateHistories.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
