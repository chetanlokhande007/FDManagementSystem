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

            decimal totalInterest;
            decimal maturityAmount;

            if (isCompounding)
            {
                maturityAmount = maturityRow?.CashFlowAmount ?? principal;
                totalInterest = Math.Round(maturityAmount - principal, 2, MidpointRounding.AwayFromZero);
            }
            else
            {
                // Non-Compounding: Sum of all periodic payouts; Maturity pays back principal
                totalInterest = records.Where(r => r.Event == "Interest").Sum(r => r.InterestAmount);
                maturityAmount = maturityRow?.CashFlowAmount ?? principal;
            }

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
                InterestFrequency = interest?.InterestFrequency ?? "Monthly",
                CompoundingFrequency = interest?.CompoundingFrequency ?? "Not Applicable",
                IsCompounding = isCompounding,
                CalculationBasis = interest?.CalculationBasis ?? "ACTUAL_365",
                TotalTenorDays = totalDays,
                TotalInterest = Math.Round(totalInterest, 2),
                MaturityAmount = Math.Round(maturityAmount, 2),
                Schedule = dtos
            };
        }

        private List<FDCashFlow> GenerateCashFlows(FDIdentification fd, FDInterest interest)
        {
            decimal effectiveRate = GetEffectiveInterestRate(interest);
            var cashFlows = new List<FDCashFlow>();
            var now = DateTime.UtcNow;

            DateTime startDate = fd.StartDate.Date;
            DateTime maturityDate = fd.EndDate.Date;

            // 1. Initial Deposit
            cashFlows.Add(new FDCashFlow
            {
                FdId = fd.FdId,
                Event = "FD Created",
                StartDate = startDate,
                EndDate = startDate,
                Days = 0,
                InterestRate = effectiveRate,
                OpeningBalance = 0m,
                InterestAmount = 0m,
                ClosingBalance = fd.PrincipalAmount,
                CashFlowAmount = fd.PrincipalAmount,
                Direction = "OUTFLOW",
                CurrencyCode = fd.CurrencyCode ?? "INR",
                Status = "PENDING",
                ReferenceNo = fd.FdReferenceNo ?? "",
                CreatedDate = now
            });

            decimal balance = fd.PrincipalAmount;
            decimal accumulatedAccrual = 0m;
            DateTime currentPeriodStart = startDate;
            DateTime lastCompoundingStartDate = startDate;

            bool isCompounding = interest.IsCompounding;
            int? compoundingMonths = isCompounding ? GetFrequencyMonths(interest.CompoundingFrequency) : null;
            DateTime nextCompoundingDate = (isCompounding && compoundingMonths.HasValue)
                ? GetTargetFrequencyEndDate(startDate, compoundingMonths.Value, maturityDate)
                : maturityDate;

            // 2. Interest and Compounding Schedule
            while (currentPeriodStart < maturityDate)
            {
                DateTime periodEnd = GetNextInterestPeriodEnd(currentPeriodStart, interest.InterestFrequency, maturityDate);
                int days = (periodEnd - currentPeriodStart).Days + (periodEnd == maturityDate ? 0 : 1);

                decimal periodInterest = 0m;
                if (days > 0)
                {
                    periodInterest = FinTrustFDManager.BAL.Common.FinancialCalculator.CalculateInterest(
                        balance,
                        effectiveRate,
                        days,
                        interest.CalculationBasis);

                    periodInterest = Math.Round(periodInterest, 2, MidpointRounding.AwayFromZero);
                    accumulatedAccrual += periodInterest;
                }

                cashFlows.Add(new FDCashFlow
                {
                    FdId = fd.FdId,
                    Event = "Interest",
                    StartDate = currentPeriodStart,
                    EndDate = periodEnd,
                    Days = days,
                    InterestRate = effectiveRate,
                    OpeningBalance = balance,
                    InterestAmount = periodInterest,
                    ClosingBalance = balance,
                    CashFlowAmount = isCompounding ? 0m : periodInterest,
                    Direction = "INFLOW",
                    CurrencyCode = fd.CurrencyCode ?? "INR",
                    Status = "PENDING",
                    ReferenceNo = fd.FdReferenceNo ?? "",
                    CreatedDate = now
                });

                DateTime nextPeriodStart = periodEnd.AddDays(1);

                if (isCompounding && (periodEnd == nextCompoundingDate || nextPeriodStart > nextCompoundingDate) && periodEnd < maturityDate)
                {
                    int compoundingDays = (periodEnd - lastCompoundingStartDate).Days + 1;
                    decimal compoundedAmount = accumulatedAccrual;
                    decimal newBalance = balance + compoundedAmount;

                    cashFlows.Add(new FDCashFlow
                    {
                        FdId = fd.FdId,
                        Event = "Compounding Interest",
                        StartDate = lastCompoundingStartDate,
                        EndDate = periodEnd,
                        Days = compoundingDays,
                        InterestRate = effectiveRate,
                        OpeningBalance = balance,
                        InterestAmount = compoundedAmount,
                        ClosingBalance = newBalance,
                        CashFlowAmount = 0m,
                        Direction = "INFLOW",
                        CurrencyCode = fd.CurrencyCode ?? "INR",
                        Status = "PENDING",
                        ReferenceNo = fd.FdReferenceNo ?? "",
                        CreatedDate = now
                    });

                    balance = newBalance;
                    accumulatedAccrual = 0m;
                    lastCompoundingStartDate = nextPeriodStart;

                    if (compoundingMonths.HasValue)
                    {
                        nextCompoundingDate = GetTargetFrequencyEndDate(nextPeriodStart, compoundingMonths.Value, maturityDate);
                    }
                }
                else if (!isCompounding)
                {
                    accumulatedAccrual = 0m;
                }

                currentPeriodStart = nextPeriodStart;
            }

            // 3. Maturity Settlement
            decimal finalMaturityPayout = balance + (isCompounding ? accumulatedAccrual : 0m);

            cashFlows.Add(new FDCashFlow
            {
                FdId = fd.FdId,
                Event = "Maturity",
                StartDate = maturityDate,
                EndDate = maturityDate,
                Days = 0,
                InterestRate = effectiveRate,
                OpeningBalance = balance,
                InterestAmount = accumulatedAccrual,
                ClosingBalance = 0m,
                CashFlowAmount = finalMaturityPayout,
                Direction = "INFLOW",
                CurrencyCode = fd.CurrencyCode ?? "INR",
                Status = "PENDING",
                ReferenceNo = fd.FdReferenceNo ?? "",
                CreatedDate = now
            });

            // PostgreSQL UTC date normalization
            foreach (var cf in cashFlows)
            {
                cf.StartDate = DateTime.SpecifyKind(cf.StartDate.Date, DateTimeKind.Utc);
                cf.EndDate = DateTime.SpecifyKind(cf.EndDate.Date, DateTimeKind.Utc);
                cf.CreatedDate = DateTime.UtcNow;
            }

            return cashFlows;
        }

        private static DateTime GetNextInterestPeriodEnd(DateTime periodStart, string frequency, DateTime maxDate)
        {
            var normalized = frequency?.Trim().ToUpperInvariant().Replace("-", "_").Replace(" ", "_");
            if (normalized == "AT_MATURITY") return maxDate;

            int months = GetFrequencyMonths(frequency) ?? 1;
            return GetTargetFrequencyEndDate(periodStart, months, maxDate);
        }

        private static DateTime GetTargetFrequencyEndDate(DateTime windowStart, int months, DateTime maxDate)
        {
            DateTime targetMonth = windowStart.AddMonths(months - 1);
            int daysInMonth = DateTime.DaysInMonth(targetMonth.Year, targetMonth.Month);
            DateTime periodEnd = new DateTime(targetMonth.Year, targetMonth.Month, daysInMonth);
            return periodEnd > maxDate ? maxDate : periodEnd;
        }

        private static int? GetFrequencyMonths(string? frequency)
        {
            if (string.IsNullOrWhiteSpace(frequency)) return null;

            var normalized = frequency.Trim().ToUpperInvariant()
                .Replace("-", "_")
                .Replace(" ", "_");

            return normalized switch
            {
                "MONTHLY" or "MONTH" => 1,
                "QUARTERLY" or "QUARTER" => 3,
                "HALF_YEARLY" or "HALFYEARLY" or "SEMI_ANNUAL" or "SEMIANNUAL" or "SEMI_ANNUALLY" or "SEMIANNUALLY" => 6,
                "ANNUALLY" or "ANNUAL" or "YEARLY" or "YEAR" => 12,
                "AT_MATURITY" or "ATMATURITY" => null,
                _ => null
            };
        }

        private static void ValidateInterestConfiguration(FDInterest model)
        {
            if (string.IsNullOrWhiteSpace(model.InterestRateType))
                throw new InvalidOperationException("Interest Rate Type is required.");

            var rateType = model.InterestRateType.Trim().ToUpperInvariant();
            if (rateType != "FIXED" && rateType != "FLOATING")
                throw new InvalidOperationException($"Unsupported Interest Rate Type '{model.InterestRateType}'.");

            if (string.IsNullOrWhiteSpace(model.CalculationBasis))
                throw new InvalidOperationException("Calculation Basis is required.");

            var basis = model.CalculationBasis.Trim().ToUpperInvariant();
            if (basis != "ACTUAL_360" && basis != "ACTUAL_365")
                throw new InvalidOperationException($"Unsupported Calculation Basis '{model.CalculationBasis}'.");

            if (rateType == "FIXED" && model.InterestRate <= 0)
                throw new InvalidOperationException("Interest Rate must be greater than 0 for FIXED deposits.");

            if (model.IsCompounding)
            {
                if (string.IsNullOrWhiteSpace(model.CompoundingFrequency) ||
                    model.CompoundingFrequency.Equals("Not Applicable", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Compounding Frequency is required when compounding is enabled.");
                }
            }
            else
            {
                model.CompoundingFrequency = "Not Applicable";
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