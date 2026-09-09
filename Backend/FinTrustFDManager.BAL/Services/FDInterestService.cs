using FinTrustFDManager.BAL.DTOs;
using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities.Investment;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinTrustFDManager.BAL.Services
{
    public class FDInterestService : IFDInterestService
    {
        private readonly IFDInterestRepository _interestRepository;
        private readonly IFDIdentificationRepository _fdRepository;
        private readonly IFDCashFlowRepository _cashFlowRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<FDInterestService> _logger;

        public FDInterestService(
            IFDInterestRepository interestRepository,
            IFDIdentificationRepository fdRepository,
            IFDCashFlowRepository cashFlowRepository,
            IUnitOfWork unitOfWork,
            ILogger<FDInterestService> logger)
        {
            _interestRepository = interestRepository;
            _fdRepository = fdRepository;
            _cashFlowRepository = cashFlowRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IEnumerable<FDInterest>> GetAllAsync()
        {
            return await _interestRepository.GetAllAsync();
        }

        public async Task<FDInterest?> GetByIdAsync(long id)
        {
            return await _interestRepository.GetByIdAsync(id);
        }

        public async Task<FDInterest?> GetByFdIdAsync(long fdId)
        {
            return await _interestRepository.GetByFdIdAsync(fdId);
        }

        public async Task<FDInterest> CreateAsync(FDInterest model)
        {
            ValidateInterestConfiguration(model);

            var fd = await _fdRepository.GetByIdAsync(model.FdId);
            if (fd == null)
                throw new KeyNotFoundException($"FD with ID {model.FdId} not found.");

            ValidateFdDates(fd);

            var existing = await _interestRepository.GetByFdIdAsync(model.FdId);
            if (existing != null)
                throw new InvalidOperationException($"Interest already exists for FD ID {model.FdId}.");

            model.CreatedDate = DateTime.UtcNow;

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var interest = await _interestRepository.AddAsync(model);
                var cashFlows = GenerateCashFlows(fd, interest);

                await _cashFlowRepository.AddRangeAsync(cashFlows);
                await _unitOfWork.CommitTransactionAsync();

                return interest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create interest config for FD {FdId}.", model.FdId);
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<FDInterest?> UpdateAsync(long id, FDInterest model)
        {
            ValidateInterestConfiguration(model);

            var existingInterest = await _interestRepository.GetByIdAsync(id);
            if (existingInterest == null) return null;

            var fd = await _fdRepository.GetByIdAsync(existingInterest.FdId);
            if (fd == null)
                throw new KeyNotFoundException($"FD with ID {existingInterest.FdId} not found.");

            ValidateFdDates(fd);

            model.FdInterestId = id;
            model.FdId = existingInterest.FdId;

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var updatedInterest = await _interestRepository.UpdateAsync(model);
                if (updatedInterest != null)
                {
                    var existingCashFlows = (await _cashFlowRepository.GetByFdIdAsync(fd.FdId)).ToList();
                    if (existingCashFlows.Count > 0)
                    {
                        await _cashFlowRepository.DeleteRangeAsync(existingCashFlows);
                    }

                    var newCashFlows = GenerateCashFlows(fd, updatedInterest);
                    await _cashFlowRepository.AddRangeAsync(newCashFlows);
                }

                await _unitOfWork.CommitTransactionAsync();
                return updatedInterest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update interest config {InterestId} for FD {FdId}.", id, existingInterest.FdId);
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var existingInterest = await _interestRepository.GetByIdAsync(id);
            if (existingInterest == null) return false;

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var existingCashFlows = (await _cashFlowRepository.GetByFdIdAsync(existingInterest.FdId)).ToList();
                if (existingCashFlows.Count > 0)
                {
                    await _cashFlowRepository.DeleteRangeAsync(existingCashFlows);
                }

                var result = await _interestRepository.DeleteAsync(id);
                await _unitOfWork.CommitTransactionAsync();
                return result;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> RegenerateCashFlowsAsync(long fdId)
        {
            var fd = await _fdRepository.GetByIdAsync(fdId);
            if (fd == null) return false;

            var interest = await _interestRepository.GetByFdIdAsync(fdId);
            if (interest == null) return false;

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var existingCashFlows = (await _cashFlowRepository.GetByFdIdAsync(fdId)).ToList();
                if (existingCashFlows.Count > 0)
                {
                    await _cashFlowRepository.DeleteRangeAsync(existingCashFlows);
                }

                var newCashFlows = GenerateCashFlows(fd, interest);
                await _cashFlowRepository.AddRangeAsync(newCashFlows);

                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to regenerate cash flows for FD {FdId}.", fdId);
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<FDCashFlowSummaryDto> GetSummaryAsync(long fdId)
        {
            var fd = await _fdRepository.GetByIdAsync(fdId);
            if (fd == null)
                throw new KeyNotFoundException($"FD with ID {fdId} not found.");

            var interest = await _interestRepository.GetByFdIdAsync(fdId);
            var records = (await _cashFlowRepository.GetByFdIdAsync(fdId))
                .OrderBy(c => c.StartDate)
                .ThenBy(c => c.CreatedDate)
                .ToList();

            decimal principal = fd.PrincipalAmount;
            bool isCompounding = interest?.IsCompounding ?? false;
            var maturityRow = records.FirstOrDefault(r => r.Event == "Maturity");

            decimal totalInterest = records.Where(r => r.Event != "FD Created").Sum(r => r.InterestAmount);
            decimal maturityAmount = maturityRow?.CashFlowAmount ?? principal;

            int totalDays = (fd.EndDate.Date - fd.StartDate.Date).Days;
            decimal effectiveRate = interest != null
                ? (string.Equals(interest.InterestRateType, "FLOATING", System.StringComparison.OrdinalIgnoreCase)
                    ? (interest.BenchmarkRate ?? 0) + (interest.Margin ?? 0)
                    : interest.InterestRate)
                : 0m;

            var dtos = records.Select(x => new FDCashFlowDto
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

            return new FDCashFlowSummaryDto
            {
                FdId = fdId,
                FdReferenceNo = fd.FdReferenceNo ?? $"FD-{fdId:D4}",
                PrincipalAmount = principal,
                InterestRate = effectiveRate,
                InterestRateType = interest?.InterestRateType ?? "FIXED",
                InterestFrequency = interest?.InterestFrequency?.FrequencyName ?? "Monthly",
                CompoundingFrequency = interest?.CompoundingFrequencyNavigation?.FrequencyName ?? "Not Applicable",
                IsCompounding = isCompounding,
                CalculationBasis = interest?.DayCountConvention?.ConventionName ?? "ACTUAL_365",
                TotalTenorDays = totalDays,
                TotalInterest = Math.Round(totalInterest, 2),
                MaturityAmount = Math.Round(maturityAmount, 2),
                Schedule = dtos
            };
        }

        private List<FDCashFlow> GenerateCashFlows(FDIdentification fd, FDInterest interest)
        {
            return FinTrustFDManager.BAL.Common.FDScheduleEngine.GenerateSchedule(fd, interest);
        }


        private static void ValidateInterestConfiguration(FDInterest model)
        {
            if (string.IsNullOrWhiteSpace(model.InterestRateType))
                throw new InvalidOperationException("Interest Rate Type is required.");

            var rateType = model.InterestRateType.Trim().ToUpperInvariant();
            if (rateType != "FIXED" && rateType != "FLOATING")
                throw new InvalidOperationException($"Unsupported Interest Rate Type '{model.InterestRateType}'.");

            if (model.InterestFrequencyId <= 0)
                throw new InvalidOperationException("Interest Frequency is required.");

            if (model.DayCountConventionId <= 0)
                throw new InvalidOperationException("Day Count Convention is required.");

            if (rateType == "FIXED" && model.InterestRate <= 0)
                throw new InvalidOperationException("Interest Rate must be greater than 0 for FIXED deposits.");

            if (rateType == "FLOATING")
            {
                if (!model.BenchmarkId.HasValue || model.BenchmarkId.Value <= 0)
                    throw new InvalidOperationException("Benchmark is required for FLOATING rate type.");
                if (!model.Margin.HasValue || model.Margin.Value < 0)
                    throw new InvalidOperationException("Margin is required for FLOATING rate type.");
            }

            if (model.IsCompounding && (!model.CompoundingFrequencyId.HasValue || model.CompoundingFrequencyId.Value <= 0))
            {
                throw new InvalidOperationException("Compounding Frequency is required when compounding is enabled.");
            }
        }

        private static void ValidateFdDates(FDIdentification fd)
        {
            if (fd.StartDate >= fd.EndDate)
                throw new InvalidOperationException($"FD Start Date must be before End Date.");
        }

        private static decimal GetEffectiveInterestRate(FDInterest interest)
        {
            if (string.Equals(interest.InterestRateType, "FLOATING", StringComparison.OrdinalIgnoreCase))
            {
                return (interest.BenchmarkRate ?? 0m) + (interest.Margin ?? 0m);
            }
            return interest.InterestRate;
        }
    }
}