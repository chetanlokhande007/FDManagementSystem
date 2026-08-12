using FinTrustFDManager.Model.DTOs.Entity;

namespace FinTrustFDManager.BAL.Interfaces
{
    public interface IEntityService
    {
        Task<List<EntityDto>> GetAllAsync();

        Task<EntityDto?> GetByIdAsync(int id);

        Task<EntityDto> CreateAsync(CreateEntityDto dto);

        Task<EntityDto?> UpdateAsync(
            int id,
            UpdateEntityDto dto);

        Task<bool> DeleteAsync(int id);

        Task<EntityDto?> ApproveAsync(int id);

        Task<EntityDto?> RejectAsync(int id);
    }
}
