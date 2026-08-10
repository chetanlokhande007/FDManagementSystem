using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.DTOs.BankAccount;
using FinTrustFDManager.Model.Entities.MasterData;

namespace FinTrustFDManager.BAL.Services
{
    public class BankAccountService : IBankAccountService
    {
        private readonly IBankAccountRepository _repository;

        public BankAccountService(
            IBankAccountRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<BankAccountDto>> GetAllAsync()
        {
            var accounts =
                await _repository.GetAllAsync();

            return accounts
                .Select(MapToDto)
                .ToList();
        }

        public async Task<BankAccountDto?> GetByIdAsync(
            int id)
        {
            var account =
                await _repository.GetByIdAsync(id);

            if (account == null)
            {
                return null;
            }

            return MapToDto(account);
        }

        public async Task<BankAccountDto> CreateAsync(
            CreateBankAccountDto dto)
        {
            var existing =
                await _repository.GetByAccountNumberAsync(
                    dto.AccountNumber);

            if (existing != null)
            {
                throw new InvalidOperationException(
                    "Bank account number already exists.");
            }

            var account = new BankAccount
            {
                BankId = dto.BankId,
                AccountNumber = dto.AccountNumber,
                AccountName = dto.AccountName,
                CurrencyId = dto.CurrencyId,
                IsActive = dto.IsActive
            };

            var created =
                await _repository.CreateAsync(account);

            var result =
                await _repository.GetByIdAsync(created.Id);

            return MapToDto(result!);
        }

        public async Task<BankAccountDto?> UpdateAsync(
            int id,
            UpdateBankAccountDto dto)
        {
            var account =
                await _repository.GetByIdAsync(id);

            if (account == null)
            {
                return null;
            }

            var existing =
                await _repository.GetByAccountNumberAsync(
                    dto.AccountNumber);

            if (existing != null &&
                existing.Id != id)
            {
                throw new InvalidOperationException(
                    "Bank account number already exists.");
            }

            account.BankId = dto.BankId;
            account.AccountNumber = dto.AccountNumber;
            account.AccountName = dto.AccountName;
            account.CurrencyId = dto.CurrencyId;
            account.IsActive = dto.IsActive;

            await _repository.UpdateAsync(account);

            var updated =
                await _repository.GetByIdAsync(id);

            return MapToDto(updated!);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        private static BankAccountDto MapToDto(
            BankAccount account)
        {
            return new BankAccountDto
            {
                Id = account.Id,

                BankId = account.BankId,
                BankName = account.Bank?.BankName,

                AccountNumber = account.AccountNumber,
                AccountName = account.AccountName,

                CurrencyId = account.CurrencyId,
                CurrencyName = account.Currency?.CurrencyName,

                IsActive = account.IsActive
            };
        }
    }
}
