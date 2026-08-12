using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.DTOs.CounterParty;
using FinTrustFDManager.Model.Entities;

namespace FinTrustFDManager.BAL.Services
{
    public class CounterPartyService : ICounterPartyService
    {
        private readonly ICounterPartyRepository _repository;

        public CounterPartyService(
            ICounterPartyRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<CounterPartyDto>> GetAllAsync()
        {
            var counterParties =
                await _repository.GetAllAsync();

            return counterParties
                .Select(MapToDto)
                .ToList();
        }

        public async Task<CounterPartyDto?> GetByIdAsync(
            int id)
        {
            var counterParty =
                await _repository.GetByIdAsync(id);

            if (counterParty == null)
            {
                return null;
            }

            return MapToDto(counterParty);
        }

        public async Task<CounterPartyDto> CreateAsync(
            CreateCounterPartyDto dto)
        {
            var existing = await _repository
                .GetByCodeAsync(dto.CounterPartyCode);

            if (existing != null)
            {
                throw new InvalidOperationException(
                    "Counter Party Code already exists.");
            }

            var counterParty = new CounterParty
            {
                CounterPartyCode = dto.CounterPartyCode,
                CounterPartyName = dto.CounterPartyName,
                CountryId = dto.CountryId,
                IsActive = dto.IsActive
            };

            var created = await _repository
                .CreateAsync(counterParty);

            var result = await _repository
                .GetByIdAsync(created.CounterPartyId);

            return MapToDto(result!);
        }

        public async Task<CounterPartyDto?> UpdateAsync(
            int id,
            UpdateCounterPartyDto dto)
        {
            var counterParty =
                await _repository.GetByIdAsync(id);

            if (counterParty == null)
            {
                return null;
            }

            var existing = await _repository
                .GetByCodeAsync(dto.CounterPartyCode);

            if (existing != null &&
                existing.CounterPartyId != id)
            {
                throw new InvalidOperationException(
                    "Counter Party Code already exists.");
            }

            counterParty.CounterPartyCode =
                dto.CounterPartyCode;

            counterParty.CounterPartyName =
                dto.CounterPartyName;

            counterParty.CountryId =
                dto.CountryId;

            counterParty.IsActive = dto.IsActive;

            await _repository.UpdateAsync(counterParty);

            var updated = await _repository
                .GetByIdAsync(id);

            return MapToDto(updated!);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        private static CounterPartyDto MapToDto(
            CounterParty counterParty)
        {
            return new CounterPartyDto
            {
                CounterPartyId =
                    counterParty.CounterPartyId,

                CounterPartyCode =
                    counterParty.CounterPartyCode,

                CounterPartyName =
                    counterParty.CounterPartyName,

                CountryId =
                    counterParty.CountryId,

                CountryName =
                    counterParty.Country?.CountryName,

                IsActive =
                    counterParty.IsActive
            };
        }
    }
}
