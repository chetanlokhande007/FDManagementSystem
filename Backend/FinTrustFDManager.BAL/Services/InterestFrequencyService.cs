using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.DTOs.InterestFrequency;
using FinTrustFDManager.Model.Entities.CoreData;

namespace FinTrustFDManager.BAL.Services
{
    public class InterestFrequencyService : IInterestFrequencyService
    {
        private readonly IInterestFrequencyRepository _repository;

        public InterestFrequencyService(IInterestFrequencyRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<InterestFrequencyDto>> GetAllAsync()
        {
            var list = await _repository.GetAllAsync();
            return list.Select(MapToDto).ToList();
        }

        public async Task<InterestFrequencyDto?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return null;
            return MapToDto(entity);
        }

        public async Task<InterestFrequencyDto> CreateAsync(CreateInterestFrequencyDto dto)
        {
            var entity = new InterestFrequency
            {
                FrequencyName = dto.FrequencyName,
                IsActive = dto.IsActive
            };

            var created = await _repository.CreateAsync(entity);
            return MapToDto(created);
        }

        public async Task<InterestFrequencyDto?> UpdateAsync(int id, UpdateInterestFrequencyDto dto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return null;

            entity.FrequencyName = dto.FrequencyName;
            entity.IsActive = dto.IsActive;

            var updated = await _repository.UpdateAsync(entity);
            return MapToDto(updated);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        private static InterestFrequencyDto MapToDto(InterestFrequency entity)
        {
            return new InterestFrequencyDto
            {
                Id = entity.Id,
                FrequencyName = entity.FrequencyName,
                IsActive = entity.IsActive
            };
        }
    }
}
