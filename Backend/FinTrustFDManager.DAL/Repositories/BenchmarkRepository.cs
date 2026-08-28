using FinTrustFDManager.DAL.Data;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities.MasterData;
using Microsoft.EntityFrameworkCore;

namespace FinTrustFDManager.DAL.Repositories
{
    public class BenchmarkRepository : IBenchmarkRepository
    {
        private readonly ApplicationDbContext _context;

        public BenchmarkRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Benchmark>> GetAllAsync()
        {
            return await _context.Benchmarks
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Benchmark?> GetByIdAsync(int id)
        {
            return await _context.Benchmarks
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BenchmarkId == id);
        }

        public async Task<Benchmark> AddAsync(Benchmark model)
        {
            _context.Benchmarks.Add(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<Benchmark?> UpdateAsync(Benchmark model)
        {
            var existing = await _context.Benchmarks
                .FirstOrDefaultAsync(x => x.BenchmarkId == model.BenchmarkId);

            if (existing == null)
                return null;

            existing.BenchmarkName = model.BenchmarkName;
            existing.Description = model.Description;
            existing.CurrentRate = model.CurrentRate;
            existing.RateUnit = model.RateUnit;
            existing.IsActive = model.IsActive;
            existing.ModifiedDate = DateTime.UtcNow;
            existing.ModifiedBy = model.ModifiedBy;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _context.Benchmarks
                .FirstOrDefaultAsync(x => x.BenchmarkId == id);

            if (existing == null)
                return false;

            _context.Benchmarks.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
