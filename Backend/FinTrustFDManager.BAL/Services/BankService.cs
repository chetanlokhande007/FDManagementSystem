using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.DTOs.Bank;
using FinTrustFDManager.Model.Entities;
using FinTrustFDManager.Model.Entities.MasterData;

namespace FinTrustFDManager.BAL.Services
{
    public class BankService : IBankService
    {
        private readonly IBankRepository _repository;

        public BankService(IBankRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<BankDto>> GetAllAsync()
        {
            var banks = await _repository.GetAllAsync();

            return banks
                .Select(MapToDto)
                .ToList();
        }

        public async Task<BankDto?> GetByIdAsync(int id)
        {
            var bank = await _repository.GetByIdAsync(id);

            if (bank == null)
            {
                return null;
            }

            return MapToDto(bank);
        }

        public async Task<BankDto> CreateAsync(
            CreateBankDto dto)
        {
            var existing = await _repository
                .GetByCodeAsync(dto.BankCode);

            if (existing != null)
            {
                throw new InvalidOperationException(
                    "Bank Code already exists.");
            }

            var bank = new Bank
            {
                BankCode = dto.BankCode,
                BankName = dto.BankName,
                CountryId = dto.CountryId
            };

            var created = await _repository
                .CreateAsync(bank);

            var result = await _repository
                .GetByIdAsync(created.BankId);

            return MapToDto(result!);
        }

        public async Task<BankDto?> UpdateAsync(
            int id,
            UpdateBankDto dto)
        {
            var bank = await _repository.GetByIdAsync(id);

            if (bank == null)
            {
                return null;
            }

            var existing = await _repository
                .GetByCodeAsync(dto.BankCode);

            if (existing != null &&
                existing.BankId != id)
            {
                throw new InvalidOperationException(
                    "Bank Code already exists.");
            }

            bank.BankCode = dto.BankCode;
            bank.BankName = dto.BankName;
            bank.CountryId = dto.CountryId;

            await _repository.UpdateAsync(bank);

            var updated = await _repository
                .GetByIdAsync(id);

            return MapToDto(updated!);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        private static BankDto MapToDto(Bank bank)
        {
            return new BankDto
            {
                BankId = bank.BankId,
                BankCode = bank.BankCode,
                BankName = bank.BankName,
                CountryId = bank.CountryId,
                CountryName = bank.Country?.CountryName
            };
        }
    }
}
