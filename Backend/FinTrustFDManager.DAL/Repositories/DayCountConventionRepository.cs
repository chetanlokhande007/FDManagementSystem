using FinTrustFDManager.DAL.Data;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities.CoreData;
using Microsoft.EntityFrameworkCore;

namespace FinTrustFDManager.DAL.Repositories
{
    public class DayCountConventionRepository : IDayCountConventionRepository
    {
        private readonly ApplicationDbContext _context;

        public DayCountConventionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<DayCountConvention>> GetAllAsync()
        {
            return await _context.DayCountConventions
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<DayCountConvention?> GetByIdAsync(int id)
        {
            return await _context.DayCountConventions
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<DayCountConvention> CreateAsync(DayCountConvention entity)
        {
            _context.DayCountConventions.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<DayCountConvention> UpdateAsync(DayCountConvention entity)
        {
            _context.DayCountConventions.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.DayCountConventions
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.DayCountConventions.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
