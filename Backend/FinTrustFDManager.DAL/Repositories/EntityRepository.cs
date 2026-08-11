using FinTrustFDManager.DAL.Data;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinTrustFDManager.DAL.Repositories
{
    public class EntityRepository : IEntityRepository
    {
        private readonly ApplicationDbContext _context;

        public EntityRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Entity>> GetAllAsync()
        {
            return await _context.Entities
                .Include(x => x.Country)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Entity?> GetByIdAsync(int id)
        {
            return await _context.Entities
                .Include(x => x.Country)
                .FirstOrDefaultAsync(x => x.EntityId == id);
        }

        public async Task<Entity?> GetByCodeAsync(string code)
        {
            return await _context.Entities
                .FirstOrDefaultAsync(x => x.EntityCode == code);
        }

        public async Task<Entity> CreateAsync(Entity entity)
        {
            _context.Entities.Add(entity);

            await _context.SaveChangesAsync();

            return entity;
        }

        public async Task<Entity> UpdateAsync(Entity entity)
        {
            _context.Entities.Update(entity);

            await _context.SaveChangesAsync();

            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Entities
                .FirstOrDefaultAsync(x => x.EntityId == id);

            if (entity == null)
            {
                return false;
            }

            _context.Entities.Remove(entity);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}