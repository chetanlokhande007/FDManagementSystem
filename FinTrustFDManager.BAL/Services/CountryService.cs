using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.DTOs.Country;
using FinTrustFDManager.Model.Entities;

namespace FinTrustFDManager.BAL.Services
{
    public class CountryService : ICountryService
    {
        private readonly ICountryRepository _repository;

        public CountryService(ICountryRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<CountryDto>> GetAllAsync()
        {
            var countries = await _repository.GetAllAsync();

            return countries
                .Select(MapToDto)
                .ToList();
        }

        public async Task<CountryDto?> GetByIdAsync(int id)
        {
            var country = await _repository.GetByIdAsync(id);

            if (country == null)
            {
                return null;
            }

            return MapToDto(country);
        }

        public async Task<CountryDto> CreateAsync(
            CreateCountryDto dto)
        {
            var existing = await _repository
                .GetByCodeAsync(dto.CountryCode);

            if (existing != null)
            {
                throw new InvalidOperationException(
                    "Country Code already exists.");
            }

            var country = new Country
            {
                CountryCode = dto.CountryCode,
                CountryName = dto.CountryName,
                Description = dto.Description
            };

            var created = await _repository
                .CreateAsync(country);

            return MapToDto(created);
        }

        public async Task<CountryDto?> UpdateAsync(
            int id,
            UpdateCountryDto dto)
        {
            var country = await _repository
                .GetByIdAsync(id);

            if (country == null)
            {
                return null;
            }

            var existing = await _repository
                .GetByCodeAsync(dto.CountryCode);

            if (existing != null &&
                existing.CountryId != id)
            {
                throw new InvalidOperationException(
                    "Country Code already exists.");
            }

            country.CountryCode = dto.CountryCode;
            country.CountryName = dto.CountryName;
            country.Description = dto.Description;

            await _repository.UpdateAsync(country);

            return MapToDto(country);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        private static CountryDto MapToDto(Country country)
        {
            return new CountryDto
            {
                CountryId = country.CountryId,
                CountryCode = country.CountryCode,
                CountryName = country.CountryName,
                Description = country.Description
            };
        }
    }
}
