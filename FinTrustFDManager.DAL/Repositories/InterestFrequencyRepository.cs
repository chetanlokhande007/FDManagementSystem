using FinTrustFDManager.DAL.Data;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities.CoreData;
using Microsoft.EntityFrameworkCore;

namespace FinTrustFDManager.DAL.Repositories
{
    public class InterestFrequencyRepository : IInterestFrequencyRepository
    {
        private readonly ApplicationDbContext _context;

        public InterestFrequencyRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<InterestFrequency>> GetAllAsync()
        {
            return await _context.InterestFrequencies
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<InterestFrequency?> GetByIdAsync(int id)
        {
            return await _context.InterestFrequencies
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<InterestFrequency> CreateAsync(InterestFrequency entity)
        {
            _context.InterestFrequencies.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<InterestFrequency> UpdateAsync(InterestFrequency entity)
        {
            _context.InterestFrequencies.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.InterestFrequencies
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.InterestFrequencies.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
