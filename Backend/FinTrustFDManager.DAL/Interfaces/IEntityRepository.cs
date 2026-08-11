using FinTrustFDManager.Model.Entities;

namespace FinTrustFDManager.DAL.Interfaces
{
    public interface IEntityRepository
    {
        Task<List<Entity>> GetAllAsync();

        Task<Entity?> GetByIdAsync(int id);

        Task<Entity?> GetByCodeAsync(string code);

        Task<Entity> CreateAsync(Entity entity);

        Task<Entity> UpdateAsync(Entity entity);

        Task<bool> DeleteAsync(int id);
    }
}
