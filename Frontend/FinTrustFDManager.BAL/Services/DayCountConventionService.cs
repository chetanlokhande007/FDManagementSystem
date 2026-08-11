using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.DTOs.DayCountConvention;
using FinTrustFDManager.Model.Entities.CoreData;

namespace FinTrustFDManager.BAL.Services
{
    public class DayCountConventionService : IDayCountConventionService
    {
        private readonly IDayCountConventionRepository _repository;

        public DayCountConventionService(IDayCountConventionRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<DayCountConventionDto>> GetAllAsync()
        {
            var list = await _repository.GetAllAsync();
            return list.Select(MapToDto).ToList();
        }

        public async Task<DayCountConventionDto?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return null;
            return MapToDto(entity);
        }

        public async Task<DayCountConventionDto> CreateAsync(CreateDayCountConventionDto dto)
        {
            var entity = new DayCountConvention
            {
                ConventionName = dto.ConventionName,
                IsActive = dto.IsActive
            };

            var created = await _repository.CreateAsync(entity);
            return MapToDto(created);
        }

        public async Task<DayCountConventionDto?> UpdateAsync(int id, UpdateDayCountConventionDto dto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return null;

            entity.ConventionName = dto.ConventionName;
            entity.IsActive = dto.IsActive;

            var updated = await _repository.UpdateAsync(entity);
            return MapToDto(updated);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        private static DayCountConventionDto MapToDto(DayCountConvention entity)
        {
            return new DayCountConventionDto
            {
                Id = entity.Id,
                ConventionName = entity.ConventionName,
                IsActive = entity.IsActive
            };
        }
    }
}
