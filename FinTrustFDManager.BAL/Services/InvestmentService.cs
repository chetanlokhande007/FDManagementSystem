using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.DTOs.Investment;
using FinTrustFDManager.Model.Entities.CoreData;

namespace FinTrustFDManager.BAL.Services
{
    public class InvestmentService : IInvestmentService
    {
        private readonly IInvestmentRepository _repository;

        public InvestmentService(IInvestmentRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<InvestmentDto>> GetAllAsync()
        {
            var list = await _repository.GetAllAsync();
            return list.Select(MapToDto).ToList();
        }

        public async Task<InvestmentDto?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return null;
            return MapToDto(entity);
        }

        public async Task<InvestmentDto> CreateAsync(CreateInvestmentDto dto)
        {
            var entity = new Investment
            {
                InvestmentReferenceNo = "INV-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                EntityId = dto.EntityId,
                CountryId = dto.CountryId,
                CurrencyId = dto.CurrencyId,
                BankId = dto.BankId,
                BankAccountId = dto.BankAccountId,
                InterestFrequencyId = dto.InterestFrequencyId,
                DayCountConventionId = dto.DayCountConventionId,
                PrincipalAmount = dto.PrincipalAmount,
                InterestRate = dto.InterestRate,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Remarks = dto.Remarks,
                Status = "Draft",
                CreatedBy = dto.CreatedBy,
                CreatedDate = DateTime.UtcNow
            };

            var created = await _repository.CreateAsync(entity);
            var result = await _repository.GetByIdAsync(created.Id);
            return MapToDto(result!);
        }

        public async Task<InvestmentDto?> UpdateAsync(int id, UpdateInvestmentDto dto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return null;

            entity.EntityId = dto.EntityId;
            entity.CountryId = dto.CountryId;
            entity.CurrencyId = dto.CurrencyId;
            entity.BankId = dto.BankId;
            entity.BankAccountId = dto.BankAccountId;
            entity.InterestFrequencyId = dto.InterestFrequencyId;
            entity.DayCountConventionId = dto.DayCountConventionId;
            entity.PrincipalAmount = dto.PrincipalAmount;
            entity.InterestRate = dto.InterestRate;
            entity.StartDate = dto.StartDate;
            entity.EndDate = dto.EndDate;
            entity.Remarks = dto.Remarks;
            entity.Status = dto.Status;
            entity.ModifiedBy = dto.ModifiedBy;
            entity.ModifiedDate = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);
            var updated = await _repository.GetByIdAsync(id);
            return MapToDto(updated!);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        private static InvestmentDto MapToDto(Investment entity)
        {
            return new InvestmentDto
            {
                Id = entity.Id,
                InvestmentReferenceNo = entity.InvestmentReferenceNo,
                EntityId = entity.EntityId,
                EntityName = entity.Entity?.EntityName,
                CountryId = entity.CountryId,
                CountryName = entity.Country?.CountryName,
                CurrencyId = entity.CurrencyId,
                CurrencyName = entity.Currency?.CurrencyName,
                BankId = entity.BankId,
                BankName = entity.Bank?.BankName,
                BankAccountId = entity.BankAccountId,
                AccountNumber = entity.BankAccount?.AccountNumber,
                InterestFrequencyId = entity.InterestFrequencyId,
                InterestFrequencyName = entity.InterestFrequency?.FrequencyName,
                DayCountConventionId = entity.DayCountConventionId,
                DayCountConventionName = entity.DayCountConvention?.ConventionName,
                PrincipalAmount = entity.PrincipalAmount,
                InterestRate = entity.InterestRate,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Remarks = entity.Remarks,
                Status = entity.Status,
                CreatedDate = entity.CreatedDate,
                CreatedBy = entity.CreatedBy,
                ModifiedDate = entity.ModifiedDate,
                ModifiedBy = entity.ModifiedBy
            };
        }
    }
}
