using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.DTOs.Currency;
using FinTrustFDManager.Model.Entities;

namespace FinTrustFDManager.BAL.Services
{
    public class CurrencyService : ICurrencyService
    {
        private readonly ICurrencyRepository _repository;

        public CurrencyService(ICurrencyRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<CurrencyDto>> GetAllAsync()
        {
            var currencies = await _repository.GetAllAsync();

            return currencies
                .Select(MapToDto)
                .ToList();
        }

        public async Task<CurrencyDto?> GetByIdAsync(int id)
        {
            var currency = await _repository.GetByIdAsync(id);

            if (currency == null)
            {
                return null;
            }

            return MapToDto(currency);
        }

        public async Task<CurrencyDto> CreateAsync(
            CreateCurrencyDto dto)
        {
            var existing = await _repository
                .GetByCodeAsync(dto.CurrencyCode);

            if (existing != null)
            {
                throw new InvalidOperationException(
                    "Currency Code already exists.");
            }

            var allCurrencies = await _repository.GetAllAsync();
            if (allCurrencies.Any(c => c.CurrencyName.Equals(dto.CurrencyName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    "Currency Name already exists.");
            }

            var currency = new Currency
            {
                CurrencyCode = dto.CurrencyCode,
                CurrencyName = dto.CurrencyName,
                Symbol = dto.Symbol,
                Description = dto.Description,
                IsActive = dto.IsActive
            };

            var created = await _repository
                .CreateAsync(currency);

            return MapToDto(created);
        }

        public async Task<CurrencyDto?> UpdateAsync(
            int id,
            UpdateCurrencyDto dto)
        {
            var currency = await _repository.GetByIdAsync(id);

            if (currency == null)
            {
                return null;
            }

            var existing = await _repository
                .GetByCodeAsync(dto.CurrencyCode);

            if (existing != null &&
                existing.CurrencyId != id)
            {
                throw new InvalidOperationException(
                    "Currency Code already exists.");
            }

            var allCurrencies = await _repository.GetAllAsync();
            if (allCurrencies.Any(c => c.CurrencyName.Equals(dto.CurrencyName, StringComparison.OrdinalIgnoreCase) && c.CurrencyId != id))
            {
                throw new InvalidOperationException(
                    "Currency Name already exists.");
            }

            currency.CurrencyCode = dto.CurrencyCode;
            currency.CurrencyName = dto.CurrencyName;
            currency.Symbol = dto.Symbol;
            currency.Description = dto.Description;
            currency.IsActive = dto.IsActive;

            await _repository.UpdateAsync(currency);

            return MapToDto(currency);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        private static CurrencyDto MapToDto(
            Currency currency)
        {
            return new CurrencyDto
            {
                CurrencyId = currency.CurrencyId,
                CurrencyCode = currency.CurrencyCode,
                CurrencyName = currency.CurrencyName,
                Symbol = currency.Symbol,
                Description = currency.Description,
                IsActive = currency.IsActive
            };
        }
    }
}
