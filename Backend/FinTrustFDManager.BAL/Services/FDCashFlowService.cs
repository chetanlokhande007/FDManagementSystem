using FinTrustFDManager.BAL.DTOs;
using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities.Investment;
using FinTrustFDManager.Model.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            return cashFlows
                .OrderBy(c => c.EndDate)
                .ThenBy(c => GetEventSortOrder(c.Event))
                .Select(x => MapToDto(x));
        }

        // GET BY ID
        public async Task<FDCashFlowDto?> GetByIdAsync(long id)
        {
            var x = await _repository.GetByIdAsync(id);

            if (x == null)
                return null;

            return MapToDto(x);
        }

        // GET BY FD ID (POPULATES ALL DYNAMIC METADATA & FINANCIAL SUMMARY WITH PROPER SORTING)
        public async Task<FDCashFlowSummaryDto> GetByFdIdAsync(long fdId)
        {
            // 1. Fetch Core FD entity & Interest config
            var fd = await _fdRepository.GetByIdAsync(fdId);
            var interest = await _interestService.GetByFdIdAsync(fdId);

            // 2. Fetch Cash Flow schedule rows sorted chronologically by EndDate and Event hierarchy
            var rawEntities = await _repository.GetByFdIdAsync(fdId);
            var cashFlowEntities = rawEntities
                .OrderBy(c => c.EndDate)
                .ThenBy(c => GetEventSortOrder(c.Event))
                .ToList();

            var cashFlows = cashFlowEntities.Select(x => MapToDto(x)).ToList();

            decimal principal = fd?.PrincipalAmount
                ?? cashFlows.FirstOrDefault(c => c.Event == "FD Created")?.CashFlowAmount
                ?? 0m;

            bool isCompounding = interest?.IsCompounding ?? false;
            var maturityRow = cashFlows.FirstOrDefault(c => c.Event == "Maturity");

            decimal totalInterest;
            decimal maturityAmount;

            if (isCompounding)
            {
                // For Compounding: Maturity includes all accumulated interest
                maturityAmount = maturityRow?.CashFlowAmount ?? principal;
                totalInterest = Math.Round(maturityAmount - principal, 2, MidpointRounding.AwayFromZero);
            }
            else
            {
                // For Non-Compounding: Sum all periodic interest payouts
                totalInterest = cashFlows
                    .Where(c => c.Event == "Interest")
                    .Sum(c => c.InterestAmount);
                maturityAmount = maturityRow?.CashFlowAmount ?? principal;
            }

            // Derive Tenor Days
            int totalDays = 0;
            if (fd != null && fd.EndDate > fd.StartDate)
            {
                totalDays = (fd.EndDate.Date - fd.StartDate.Date).Days;
            }
            else if (cashFlows.Count > 0)
            {
                var first = cashFlows.First();
                var last = cashFlows.Last();
                totalDays = (last.EndDate.Date - first.StartDate.Date).Days;
            }

            // Derive Effective Interest Rate
            decimal effectiveRate = 0m;
            if (interest != null)
            {
                if (string.Equals(interest.InterestRateType, "FLOATING", StringComparison.OrdinalIgnoreCase))
                {
                    effectiveRate = (interest.BenchmarkRate ?? 0m) + (interest.Margin ?? 0m);
                }
                else
                {
                    effectiveRate = interest.InterestRate;
                }
            }
            else if (cashFlows.Count > 1)
            {
                effectiveRate = cashFlows[1].InterestRate;
            }

            return new FDCashFlowSummaryDto
            {
                FdId = fdId,
                FdReferenceNo = fd?.FdReferenceNo ?? cashFlows.FirstOrDefault()?.ReferenceNo ?? $"FD-{fdId:D4}",
                PrincipalAmount = principal,
                InterestRate = effectiveRate,
                InterestRateType = interest?.InterestRateType ?? "FIXED",
                InterestFrequency = interest?.InterestFrequency ?? "Monthly",
                CompoundingFrequency = interest?.CompoundingFrequency ?? (isCompounding ? "Quarterly" : "Not Applicable"),
                IsCompounding = isCompounding,
                CalculationBasis = interest?.CalculationBasis ?? "ACTUAL_365",
                TotalTenorDays = totalDays,
                TotalInterest = Math.Round(totalInterest, 2),
                MaturityAmount = Math.Round(maturityAmount, 2),
                Schedule = cashFlows
            };
        }

        // CREATE
        public async Task<FDCashFlowDto> CreateAsync(FDCashFlowDto dto)
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

            var result = await _repository.CreateAsync(entity);

            dto.CashFlowId = result.CashFlowId;
            dto.CreatedDate = result.CreatedDate;

            return dto;
        }

        // UPDATE
        public async Task<FDCashFlowDto?> UpdateAsync(long id, FDCashFlowDto dto)
        {
            _logger.LogInformation(
                "Updating cash flow {CashFlowId} for FD {FdId}. " +
                "Regenerating all cash flows using authoritative engine.",
                id, dto.FdId);

            var fd = await _fdRepository.GetByIdAsync(dto.FdId);
            if (fd == null)
                throw new InvalidOperationException($"FD with ID {dto.FdId} not found.");

            if (FDStatus.IsProtected(fd.Status))
            {
                throw new InvalidOperationException(
                    $"Cannot modify cash flows for FD '{fd.FdReferenceNo}' with status '{fd.Status}'. Approved records are read-only.");
            }

            var interest = await _interestService.GetByFdIdAsync(dto.FdId);
            if (interest == null)
                throw new InvalidOperationException($"Interest configuration not found for FD ID {dto.FdId}.");

            if (dto.EndDate <= dto.StartDate)
                throw new InvalidOperationException("End Date must be after Start Date.");

            if (dto.EndDate > fd.EndDate)
                throw new InvalidOperationException("End Date cannot exceed FD Maturity Date.");

            await _interestService.RegenerateCashFlowsAsync(dto.FdId);

            _logger.LogInformation("Cash flows regenerated for FD {FdId}.", dto.FdId);

            var updatedCashFlows = (await _repository.GetByFdIdAsync(dto.FdId))
                .OrderBy(c => c.EndDate)
                .ThenBy(c => GetEventSortOrder(c.Event))
                .ToList();

            if (updatedCashFlows.Count == 0)
                return null;

            var result = updatedCashFlows.FirstOrDefault(c => c.CashFlowId != 0) ?? updatedCashFlows[0];

            return MapToDto(result);
        }

        // DELETE
        public async Task<bool> DeleteAsync(long id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing != null)
            {
                var fd = await _fdRepository.GetByIdAsync(existing.FdId);
                if (fd != null && FDStatus.IsProtected(fd.Status))
                {
                    throw new InvalidOperationException(
                        $"Cannot delete cash flow for FD '{fd.FdReferenceNo}' with status '{fd.Status}'. Approved records are read-only.");
                }
            }
            return await _repository.DeleteAsync(id);
        }

        // Helper: Event Sorting Hierarchy
        private static int GetEventSortOrder(string? eventName)
        {
            return eventName switch
            {
                "FD Created" => 0,
                "Interest" => 1,
                "Compounding Interest" => 2,
                "Maturity" => 3,
                _ => 4
            };
        }

        // Helper: DTO Mapper
        private static FDCashFlowDto MapToDto(FDCashFlow x)
        {
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
    }
}