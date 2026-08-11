using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.DTOs.Investment;
using FinTrustFDManager.Model.Entities.CoreData;

namespace FinTrustFDManager.BAL.Services
{
    public class InvestmentService : IInvestmentService
    {
        private readonly IInvestmentRepository _repository;
        private readonly IEntityRepository _entityRepository;
        private readonly ICountryRepository _countryRepository;
        private readonly ICurrencyRepository _currencyRepository;
        private readonly IBankRepository _bankRepository;

        private readonly IInterestFrequencyRepository _interestFrequencyRepository;
        private readonly IDayCountConventionRepository _dayCountConventionRepository;

        public InvestmentService(
            IInvestmentRepository repository,
            IEntityRepository entityRepository,
            ICountryRepository countryRepository,
            ICurrencyRepository currencyRepository,
            IBankRepository bankRepository,

            IInterestFrequencyRepository interestFrequencyRepository,
            IDayCountConventionRepository dayCountConventionRepository)
        {
            _repository = repository;
            _entityRepository = entityRepository;
            _countryRepository = countryRepository;
            _currencyRepository = currencyRepository;
            _bankRepository = bankRepository;

            _interestFrequencyRepository = interestFrequencyRepository;
            _dayCountConventionRepository = dayCountConventionRepository;
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
            if (await _entityRepository.GetByIdAsync(dto.EntityId) == null)
                throw new InvalidOperationException($"Entity with ID {dto.EntityId} does not exist.");
            if (await _countryRepository.GetByIdAsync(dto.CountryId) == null)
                throw new InvalidOperationException($"Country with ID {dto.CountryId} does not exist.");
            if (await _currencyRepository.GetByIdAsync(dto.CurrencyId) == null)
                throw new InvalidOperationException($"Currency with ID {dto.CurrencyId} does not exist.");
            if (await _bankRepository.GetByIdAsync(dto.BankId) == null)
                throw new InvalidOperationException($"Bank with ID {dto.BankId} does not exist.");

            if (await _interestFrequencyRepository.GetByIdAsync(dto.InterestFrequencyId) == null)
                throw new InvalidOperationException($"InterestFrequency with ID {dto.InterestFrequencyId} does not exist.");
            if (await _dayCountConventionRepository.GetByIdAsync(dto.DayCountConventionId) == null)
                throw new InvalidOperationException($"DayCountConvention with ID {dto.DayCountConventionId} does not exist.");

            var entity = new Investment
            {
                InvestmentReferenceNo = "INV-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                EntityId = dto.EntityId,
                CountryId = dto.CountryId,
                CurrencyId = dto.CurrencyId,
                BankId = dto.BankId,

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

            if (await _entityRepository.GetByIdAsync(dto.EntityId) == null)
                throw new InvalidOperationException($"Entity with ID {dto.EntityId} does not exist.");
            if (await _countryRepository.GetByIdAsync(dto.CountryId) == null)
                throw new InvalidOperationException($"Country with ID {dto.CountryId} does not exist.");
            if (await _currencyRepository.GetByIdAsync(dto.CurrencyId) == null)
                throw new InvalidOperationException($"Currency with ID {dto.CurrencyId} does not exist.");
            if (await _bankRepository.GetByIdAsync(dto.BankId) == null)
                throw new InvalidOperationException($"Bank with ID {dto.BankId} does not exist.");

            if (await _interestFrequencyRepository.GetByIdAsync(dto.InterestFrequencyId) == null)
                throw new InvalidOperationException($"InterestFrequency with ID {dto.InterestFrequencyId} does not exist.");
            if (await _dayCountConventionRepository.GetByIdAsync(dto.DayCountConventionId) == null)
                throw new InvalidOperationException($"DayCountConvention with ID {dto.DayCountConventionId} does not exist.");

            entity.EntityId = dto.EntityId;
            entity.CountryId = dto.CountryId;
            entity.CurrencyId = dto.CurrencyId;
            entity.BankId = dto.BankId;

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
