using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.DTOs.Entity;
using FinTrustFDManager.Model.Entities;

namespace FinTrustFDManager.BAL.Services
{
    public class EntityService : IEntityService
    {
        private readonly IEntityRepository _repository;

        public EntityService(IEntityRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<EntityDto>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();

            return entities.Select(MapToDto).ToList();
        }

        public async Task<EntityDto?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null)
            {
                return null;
            }

            return MapToDto(entity);
        }

        public async Task<EntityDto> CreateAsync(
            CreateEntityDto dto)
        {
            // Check duplicate Entity Code
            var existing = await _repository
                .GetByCodeAsync(dto.EntityCode);

            if (existing != null)
            {
                throw new InvalidOperationException(
                    "Entity Code already exists.");
            }

            var entity = new Entity
            {
                EntityCode = dto.EntityCode,
                EntityName = dto.EntityName,
                CountryId = dto.CountryId,
                Description = dto.Description
            };

            var created = await _repository.CreateAsync(entity);

            // Reload to get Country information
            var result = await _repository
                .GetByIdAsync(created.EntityId);

            return MapToDto(result!);
        }

        public async Task<EntityDto?> UpdateAsync(
            int id,
            UpdateEntityDto dto)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null)
            {
                return null;
            }

            // Check duplicate code
            var existing = await _repository
                .GetByCodeAsync(dto.EntityCode);

            if (existing != null &&
                existing.EntityId != id)
            {
                throw new InvalidOperationException(
                    "Entity Code already exists.");
            }

            entity.EntityCode = dto.EntityCode;
            entity.EntityName = dto.EntityName;
            entity.CountryId = dto.CountryId;
            entity.Description = dto.Description;

            await _repository.UpdateAsync(entity);

            var updated = await _repository.GetByIdAsync(id);

            return MapToDto(updated!);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        private static EntityDto MapToDto(Entity entity)
        {
            return new EntityDto
            {
                EntityId = entity.EntityId,
                EntityCode = entity.EntityCode,
                EntityName = entity.EntityName,
                CountryId = entity.CountryId,
                CountryName = entity.Country?.CountryName,
                Description = entity.Description
            };
        }
    }
}
