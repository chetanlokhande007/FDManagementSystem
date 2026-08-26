using FinTrustFDManager.BAL.DTOs;
using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities.Investment;
using Microsoft.Extensions.Logging;

namespace FinTrustFDManager.BAL.Services
{
    public class FDCashFlowService : IFDCashFlowService
    {
        private readonly IFDCashFlowRepository _repository;
        private readonly IFDInterestService _interestService;
        private readonly IFDIdentificationRepository _fdRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<FDCashFlowService> _logger;

        public FDCashFlowService(
            IFDCashFlowRepository repository,
            IFDInterestService interestService,
            IFDIdentificationRepository fdRepository,
            IUnitOfWork unitOfWork,
            ILogger<FDCashFlowService> logger)
        {
            _repository = repository;
            _interestService = interestService;
            _fdRepository = fdRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // GET ALL
        public async Task<IEnumerable<FDCashFlowDto>> GetAllAsync()
        {
            var cashFlows = await _repository.GetAllAsync();

            return cashFlows.Select(x => new FDCashFlowDto
            {
                CashFlowId = x.CashFlowId,
                FdId = x.FdId,
                Event = x.Event,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Days = x.Days,
                InterestRate = x.InterestRate,
                OpeningBalance = x.OpeningBalance,
                InterestAmount = x.InterestAmount,
                ClosingBalance = x.ClosingBalance,
                CashFlowAmount = x.CashFlowAmount,
                Direction = x.Direction,
                CurrencyCode = x.CurrencyCode,
                Status = x.Status,
                ReferenceNo = x.ReferenceNo,
                CreatedDate = x.CreatedDate
            });
        }

        // GET BY ID
        public async Task<FDCashFlowDto?> GetByIdAsync(long id)
        {
            var x = await _repository.GetByIdAsync(id);

            if (x == null)
                return null;

            return new FDCashFlowDto
            {
                CashFlowId = x.CashFlowId,
                FdId = x.FdId,
                Event = x.Event,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Days = x.Days,
                InterestRate = x.InterestRate,
                OpeningBalance = x.OpeningBalance,
                InterestAmount = x.InterestAmount,
                ClosingBalance = x.ClosingBalance,
                CashFlowAmount = x.CashFlowAmount,
                Direction = x.Direction,
                CurrencyCode = x.CurrencyCode,
                Status = x.Status,
                ReferenceNo = x.ReferenceNo,
                CreatedDate = x.CreatedDate
            };
        }

        // GET BY FD ID
        public async Task<FDCashFlowSummaryDto> GetByFdIdAsync(long fdId)
        {
            var cashFlowEntities = await _repository.GetByFdIdAsync(fdId);
            
            var cashFlows = cashFlowEntities.Select(x => new FDCashFlowDto
            {
                CashFlowId = x.CashFlowId,
                FdId = x.FdId,
                Event = x.Event,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Days = x.Days,
                InterestRate = x.InterestRate,
                OpeningBalance = x.OpeningBalance,
                InterestAmount = x.InterestAmount,
                ClosingBalance = x.ClosingBalance,
                CashFlowAmount = x.CashFlowAmount,
                Direction = x.Direction,
                CurrencyCode = x.CurrencyCode,
                Status = x.Status,
                ReferenceNo = x.ReferenceNo,
                CreatedDate = x.CreatedDate
            }).ToList();

            decimal principal = cashFlows.FirstOrDefault(c => c.Event == "FD Created")?.CashFlowAmount ?? 0;
            decimal totalInflows = cashFlows.Where(c => c.Direction == "INFLOW").Sum(c => c.CashFlowAmount);
            decimal totalInterest = totalInflows - principal;
            var maturityRow = cashFlows.FirstOrDefault(c => c.Event == "Maturity");
            decimal maturityAmount = 0;
            if (maturityRow != null)
            {
                maturityAmount = cashFlows
                    .Where(c => c.EndDate == maturityRow.EndDate && c.Direction == "INFLOW")
                    .Sum(c => c.CashFlowAmount);
            }

            return new FDCashFlowSummaryDto
            {
                FdId = fdId,
                PrincipalAmount = principal,
                TotalInterest = totalInterest,
                MaturityAmount = maturityAmount,
                CashFlows = cashFlows
            };
        }

        // CREATE
        public async Task<FDCashFlowDto> CreateAsync(
            FDCashFlowDto dto)
        {
            var entity = new FDCashFlow
            {
                FdId = dto.FdId,
                Event = dto.Event,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Days = dto.Days,
                InterestRate = dto.InterestRate,
                OpeningBalance = dto.OpeningBalance,
                InterestAmount = dto.InterestAmount,
                ClosingBalance = dto.ClosingBalance,
                CashFlowAmount = dto.CashFlowAmount,
                Direction = dto.Direction,
                CurrencyCode = dto.CurrencyCode,
                Status = dto.Status,
                ReferenceNo = dto.ReferenceNo,
                CreatedDate = DateTime.UtcNow
            };

            var result =
                await _repository.CreateAsync(entity);

            dto.CashFlowId = result.CashFlowId;
            dto.CreatedDate = result.CreatedDate;

            return dto;
        }

        // UPDATE — Regenerates cash flows using the authoritative calculation engine.
        // This ensures the same financial rules are used for initial generation
        // and any recalculation (date editing, interest config changes, etc.).
        public async Task<FDCashFlowDto?> UpdateAsync(
            long id,
            FDCashFlowDto dto)
        {
            _logger.LogInformation(
                "Updating cash flow {CashFlowId} for FD {FdId}. " +
                "Regenerating all cash flows using authoritative engine.",
                id, dto.FdId);

            var fd = await _fdRepository.GetByIdAsync(dto.FdId);
            if (fd == null)
                throw new InvalidOperationException($"FD with ID {dto.FdId} not found.");

            var interest = await _interestService.GetByFdIdAsync(dto.FdId);
            if (interest == null)
                throw new InvalidOperationException($"Interest configuration not found for FD ID {dto.FdId}.");

            // Validate the edit
            if (dto.EndDate <= dto.StartDate)
                throw new InvalidOperationException("End Date must be after Start Date.");

            if (dto.EndDate > fd.EndDate)
                throw new InvalidOperationException("End Date cannot exceed FD Maturity Date.");

            // Regenerate all cash flows using the authoritative calculation engine.
            // This is the SAME engine used for initial generation (GenerateCashFlows).
            await _interestService.RegenerateCashFlowsAsync(dto.FdId);

            _logger.LogInformation(
                "Cash flows regenerated for FD {FdId}.", dto.FdId);

            // Return the updated cash flow that matches the requested ID
            // (the IDs will be new after regeneration, so return the first matching by position)
            var updatedCashFlows = (await _repository.GetByFdIdAsync(dto.FdId))
                .OrderBy(c => c.StartDate)
                .ToList();

            if (updatedCashFlows.Count == 0)
                return null;

            // Return the first non-FD-Created cash flow (or the first one)
            var result = updatedCashFlows.FirstOrDefault(c => c.CashFlowId != 0) ?? updatedCashFlows[0];

            return new FDCashFlowDto
            {
                CashFlowId = result.CashFlowId,
                FdId = result.FdId,
                Event = result.Event,
                StartDate = result.StartDate,
                EndDate = result.EndDate,
                Days = result.Days,
                InterestRate = result.InterestRate,
                OpeningBalance = result.OpeningBalance,
                InterestAmount = result.InterestAmount,
                ClosingBalance = result.ClosingBalance,
                CashFlowAmount = result.CashFlowAmount,
                Direction = result.Direction,
                CurrencyCode = result.CurrencyCode,
                Status = result.Status,
                ReferenceNo = result.ReferenceNo,
                CreatedDate = result.CreatedDate
            };
        }

        // DELETE
        public async Task<bool> DeleteAsync(long id)
        {
            return await _repository.DeleteAsync(id);
        }
    }
}
