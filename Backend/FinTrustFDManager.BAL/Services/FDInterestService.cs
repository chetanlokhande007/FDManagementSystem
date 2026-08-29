using FinTrustFDManager.BAL.DTOs;
using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities.Investment;
using FinTrustFDManager.Model.Entities.MasterData;
using FinTrustFDManager.Model.Enums;
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
        private readonly IBenchmarkRateHistoryService _benchmarkRateHistoryService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<FDInterestService> _logger;

        public FDInterestService(
            IFDInterestRepository interestRepository,
            IFDIdentificationRepository fdRepository,
            IFDCashFlowRepository cashFlowRepository,
            IBenchmarkRateHistoryService benchmarkRateHistoryService,
            IUnitOfWork unitOfWork,
            ILogger<FDInterestService> logger)
        {
            _interestRepository = interestRepository;
            _fdRepository = fdRepository;
            _cashFlowRepository = cashFlowRepository;
            _benchmarkRateHistoryService = benchmarkRateHistoryService;
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

            await ResolveBenchmarkRateAsync(model, fd.StartDate);
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
                var cashFlows = await GenerateCashFlowsAsync(fd, interest);

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

            if (FDStatus.IsProtected(fd.Status))
            {
                throw new InvalidOperationException(
                    $"Cannot modify interest for FD '{fd.FdReferenceNo}' with status '{fd.Status}'. Approved records are read-only.");
            }

            await ResolveBenchmarkRateAsync(model, fd.StartDate);

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

                    var newCashFlows = await GenerateCashFlowsAsync(fd, updatedInterest);
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

            var fd = await _fdRepository.GetByIdAsync(existingInterest.FdId);
            if (fd != null && FDStatus.IsProtected(fd.Status))
            {
                throw new InvalidOperationException(
                    $"Cannot delete interest for FD '{fd.FdReferenceNo}' with status '{fd.Status}'. Approved records are read-only.");
            }

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

            if (FDStatus.IsProtected(fd.Status))
            {
                throw new InvalidOperationException(
                    $"Cannot regenerate cash flows for FD '{fd.FdReferenceNo}' with status '{fd.Status}'. Approved records are read-only.");
            }

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

                var newCashFlows = await GenerateCashFlowsAsync(fd, interest);
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

            var rawRecords = await _cashFlowRepository.GetByFdIdAsync(fdId);
            var records = rawRecords
                .OrderBy(c => c.EndDate)
                .ThenBy(c => GetEventSortOrder(c.Event))
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
                totalInterest = records.Where(r => r.Event == "Interest").Sum(r => r.InterestAmount);
                maturityAmount = maturityRow?.CashFlowAmount ?? principal;
            }

            int totalDays = (fd.EndDate.Date - fd.StartDate.Date).Days;
            decimal effectiveRate = interest != null
                ? (string.Equals(interest.InterestRateType, "FLOATING", StringComparison.OrdinalIgnoreCase)
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
                CompoundingFrequency = interest?.CompoundingFrequency ?? (isCompounding ? "Quarterly" : "Not Applicable"),
                IsCompounding = isCompounding,
                CalculationBasis = interest?.CalculationBasis ?? "ACTUAL_365",
                TotalTenorDays = totalDays,
                TotalInterest = Math.Round(totalInterest, 2),
                MaturityAmount = Math.Round(maturityAmount, 2),
                Schedule = dtos
            };
        }

        private async Task<List<FDCashFlow>> GenerateCashFlowsAsync(FDIdentification fd, FDInterest interest)
        {
            bool isFloating = string.Equals(interest.InterestRateType, "FLOATING", StringComparison.OrdinalIgnoreCase);
            decimal initialRate = GetEffectiveInterestRate(interest);
            var cashFlows = new List<FDCashFlow>();
            var now = DateTime.UtcNow;
            DateTime startDate = fd.StartDate.Date;
            DateTime maturityDate = fd.EndDate.Date;
            bool isCompounding = interest.IsCompounding;
            bool isATM = IsATMaturity(interest.InterestFrequency);

            // 1. Initial Deposit
            cashFlows.Add(new FDCashFlow
            {
                FdId = fd.FdId, Event = "FD Created",
                StartDate = startDate, EndDate = startDate, Days = 0,
                InterestRate = initialRate, OpeningBalance = 0m, InterestAmount = 0m,
                ClosingBalance = fd.PrincipalAmount, CashFlowAmount = fd.PrincipalAmount,
                Direction = "OUTFLOW", CurrencyCode = fd.CurrencyCode ?? "INR",
                Status = "PENDING", ReferenceNo = fd.FdReferenceNo ?? "", CreatedDate = now
            });

            decimal balance = fd.PrincipalAmount;

            if (isATM && isCompounding)
            {
                balance = await GenerateATMaturityCompounding(fd, interest, cashFlows, balance, initialRate, isFloating, startDate, maturityDate, now);
            }
            else if (isATM)
            {
                await GenerateATMaturitySimple(fd, interest, cashFlows, balance, initialRate, isFloating, startDate, maturityDate, now);
            }
            else if (isCompounding)
            {
                balance = await GenerateInterestWithCompounding(fd, interest, cashFlows, balance, initialRate, isFloating, startDate, maturityDate, now);
            }
            else
            {
                balance = await GenerateInterestOnly(fd, interest, cashFlows, balance, initialRate, isFloating, startDate, maturityDate, now);
            }

            // 3. Maturity Settlement
            cashFlows.Add(new FDCashFlow
            {
                FdId = fd.FdId, Event = "Maturity",
                StartDate = maturityDate, EndDate = maturityDate, Days = 0,
                InterestRate = initialRate, OpeningBalance = balance, InterestAmount = 0m,
                ClosingBalance = 0m, CashFlowAmount = balance,
                Direction = "INFLOW", CurrencyCode = fd.CurrencyCode ?? "INR",
                Status = "PENDING", ReferenceNo = fd.FdReferenceNo ?? "", CreatedDate = now
            });

            foreach (var cf in cashFlows)
            {
                cf.StartDate = DateTime.SpecifyKind(cf.StartDate.Date, DateTimeKind.Utc);
                cf.EndDate = DateTime.SpecifyKind(cf.EndDate.Date, DateTimeKind.Utc);
                cf.CreatedDate = DateTime.UtcNow;
            }

            return cashFlows;
        }

        private async Task<decimal> GenerateATMaturityCompounding(
            FDIdentification fd, FDInterest interest, List<FDCashFlow> cashFlows,
            decimal balance, decimal initialRate, bool isFloating,
            DateTime startDate, DateTime maturityDate, DateTime now)
        {
            int compMonths = GetFrequencyMonths(interest.CompoundingFrequency) ?? 1;
            DateTime compStart = startDate;

            while (compStart < maturityDate)
            {
                DateTime compEnd = GetTargetFrequencyEndDate(compStart, compMonths, maturityDate);
                if (compEnd > maturityDate) compEnd = maturityDate;
                int days = (compEnd - compStart).Days;
                if (days <= 0) break;

                decimal effectiveRate = initialRate;
                if (isFloating && interest.BenchmarkId.HasValue)
                {
                    decimal benchmarkRate = await _benchmarkRateHistoryService
                        .GetEffectiveRateAsync(interest.BenchmarkId.Value, compStart);
                    effectiveRate = benchmarkRate + (interest.Margin ?? 0m);
                }

                decimal periodInterest = FinTrustFDManager.BAL.Common.FinancialCalculator.CalculateInterest(
                    balance, effectiveRate, days, interest.CalculationBasis);
                periodInterest = Math.Round(periodInterest, 2, MidpointRounding.AwayFromZero);
                decimal newBalance = balance + periodInterest;

                cashFlows.Add(new FDCashFlow
                {
                    FdId = fd.FdId, Event = "Compounding Interest",
                    StartDate = compStart, EndDate = compEnd, Days = days,
                    InterestRate = effectiveRate, OpeningBalance = balance,
                    InterestAmount = periodInterest, ClosingBalance = newBalance,
                    CashFlowAmount = 0m, Direction = "INFLOW",
                    CurrencyCode = fd.CurrencyCode ?? "INR", Status = "PENDING",
                    ReferenceNo = fd.FdReferenceNo ?? "", CreatedDate = now
                });

                balance = newBalance;
                compStart = compEnd;
            }
            return balance;
        }

        private async Task GenerateATMaturitySimple(
            FDIdentification fd, FDInterest interest, List<FDCashFlow> cashFlows,
            decimal balance, decimal initialRate, bool isFloating,
            DateTime startDate, DateTime maturityDate, DateTime now)
        {
            int days = (maturityDate - startDate).Days;
            if (days <= 0) return;

            decimal effectiveRate = initialRate;
            if (isFloating && interest.BenchmarkId.HasValue)
            {
                decimal benchmarkRate = await _benchmarkRateHistoryService
                    .GetEffectiveRateAsync(interest.BenchmarkId.Value, startDate);
                effectiveRate = benchmarkRate + (interest.Margin ?? 0m);
            }

            decimal periodInterest = FinTrustFDManager.BAL.Common.FinancialCalculator.CalculateInterest(
                balance, effectiveRate, days, interest.CalculationBasis);
            periodInterest = Math.Round(periodInterest, 2, MidpointRounding.AwayFromZero);

            cashFlows.Add(new FDCashFlow
            {
                FdId = fd.FdId, Event = "Interest",
                StartDate = startDate, EndDate = maturityDate, Days = days,
                InterestRate = effectiveRate, OpeningBalance = balance,
                InterestAmount = periodInterest, ClosingBalance = balance,
                CashFlowAmount = periodInterest, Direction = "INFLOW",
                CurrencyCode = fd.CurrencyCode ?? "INR", Status = "PENDING",
                ReferenceNo = fd.FdReferenceNo ?? "", CreatedDate = now
            });
        }

        private async Task<decimal> GenerateInterestOnly(
            FDIdentification fd, FDInterest interest, List<FDCashFlow> cashFlows,
            decimal balance, decimal initialRate, bool isFloating,
            DateTime startDate, DateTime maturityDate, DateTime now)
        {
            DateTime currentStart = startDate;
            while (currentStart < maturityDate)
            {
                DateTime periodEnd = GetNextInterestPeriodEnd(currentStart, interest.InterestFrequency, maturityDate);
                int days = (periodEnd - currentStart).Days + 1;
                decimal effectiveRate = initialRate;
                if (isFloating && interest.BenchmarkId.HasValue)
                {
                    decimal benchmarkRate = await _benchmarkRateHistoryService
                        .GetEffectiveRateAsync(interest.BenchmarkId.Value, currentStart);
                    effectiveRate = benchmarkRate + (interest.Margin ?? 0m);
                }
                decimal periodInterest = 0m;
                if (days > 0)
                {
                    periodInterest = FinTrustFDManager.BAL.Common.FinancialCalculator.CalculateInterest(
                        balance, effectiveRate, days, interest.CalculationBasis);
                    periodInterest = Math.Round(periodInterest, 2, MidpointRounding.AwayFromZero);
                }
                cashFlows.Add(new FDCashFlow
                {
                    FdId = fd.FdId, Event = "Interest",
                    StartDate = currentStart, EndDate = periodEnd, Days = days,
                    InterestRate = effectiveRate, OpeningBalance = balance,
                    InterestAmount = periodInterest, ClosingBalance = balance,
                    CashFlowAmount = periodInterest, Direction = "INFLOW",
                    CurrencyCode = fd.CurrencyCode ?? "INR", Status = "PENDING",
                    ReferenceNo = fd.FdReferenceNo ?? "", CreatedDate = now
                });
                currentStart = periodEnd.AddDays(1);
            }
            return balance;
        }

        private async Task<decimal> GenerateInterestWithCompounding(
            FDIdentification fd, FDInterest interest, List<FDCashFlow> cashFlows,
            decimal balance, decimal initialRate, bool isFloating,
            DateTime startDate, DateTime maturityDate, DateTime now)
        {
            int compMonths = GetFrequencyMonths(interest.CompoundingFrequency) ?? 1;
            DateTime compStart = startDate;
            DateTime nextCompEnd = GetTargetFrequencyEndDate(startDate, compMonths, maturityDate);
            if (nextCompEnd > maturityDate) nextCompEnd = maturityDate;
            DateTime intStart = startDate;

            while (intStart < maturityDate || compStart < maturityDate)
            {
                DateTime effectiveCompEnd = nextCompEnd;
                bool hasComp = compStart < maturityDate;
                DateTime intEnd = maturityDate;
                bool hasInt = intStart < maturityDate;
                if (hasInt)
                    intEnd = GetNextInterestPeriodEnd(intStart, interest.InterestFrequency, maturityDate);
                bool compFirst = hasComp && (!hasInt || effectiveCompEnd <= intEnd);

                if (compFirst)
                {
                    int days = (effectiveCompEnd - compStart).Days;
                    if (days > 0)
                    {
                        decimal effectiveRate = initialRate;
                        if (isFloating && interest.BenchmarkId.HasValue)
                        {
                            decimal benchmarkRate = await _benchmarkRateHistoryService
                                .GetEffectiveRateAsync(interest.BenchmarkId.Value, compStart);
                            effectiveRate = benchmarkRate + (interest.Margin ?? 0m);
                        }
                        decimal periodInterest = FinTrustFDManager.BAL.Common.FinancialCalculator.CalculateInterest(
                            balance, effectiveRate, days, interest.CalculationBasis);
                        periodInterest = Math.Round(periodInterest, 2, MidpointRounding.AwayFromZero);
                        decimal newBalance = balance + periodInterest;
                        cashFlows.Add(new FDCashFlow
                        {
                            FdId = fd.FdId, Event = "Compounding Interest",
                            StartDate = compStart, EndDate = effectiveCompEnd, Days = days,
                            InterestRate = effectiveRate, OpeningBalance = balance,
                            InterestAmount = periodInterest, ClosingBalance = newBalance,
                            CashFlowAmount = 0m, Direction = "INFLOW",
                            CurrencyCode = fd.CurrencyCode ?? "INR", Status = "PENDING",
                            ReferenceNo = fd.FdReferenceNo ?? "", CreatedDate = now
                        });
                        balance = newBalance;
                    }
                    compStart = effectiveCompEnd;
                    if (compStart < maturityDate)
                        nextCompEnd = GetTargetFrequencyEndDate(compStart, compMonths, maturityDate);
                    if (nextCompEnd > maturityDate) nextCompEnd = maturityDate;
                }
                else if (hasInt)
                {
                    int days = (intEnd - intStart).Days + 1;
                    decimal effectiveRate = initialRate;
                    if (isFloating && interest.BenchmarkId.HasValue)
                    {
                        decimal benchmarkRate = await _benchmarkRateHistoryService
                            .GetEffectiveRateAsync(interest.BenchmarkId.Value, intStart);
                        effectiveRate = benchmarkRate + (interest.Margin ?? 0m);
                    }
                    decimal periodInterest = 0m;
                    if (days > 0)
                    {
                        periodInterest = FinTrustFDManager.BAL.Common.FinancialCalculator.CalculateInterest(
                            balance, effectiveRate, days, interest.CalculationBasis);
                        periodInterest = Math.Round(periodInterest, 2, MidpointRounding.AwayFromZero);
                    }
                    cashFlows.Add(new FDCashFlow
                    {
                        FdId = fd.FdId, Event = "Interest",
                        StartDate = intStart, EndDate = intEnd, Days = days,
                        InterestRate = effectiveRate, OpeningBalance = balance,
                        InterestAmount = periodInterest, ClosingBalance = balance,
                        CashFlowAmount = 0m, Direction = "INFLOW",
                        CurrencyCode = fd.CurrencyCode ?? "INR", Status = "PENDING",
                        ReferenceNo = fd.FdReferenceNo ?? "", CreatedDate = now
                    });
                    intStart = intEnd.AddDays(1);
                }
                else
                {
                    break;
                }
            }
            return balance;
        }

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

        private static bool IsATMaturity(string? frequency)
        {
            var normalized = frequency?.Trim().ToUpperInvariant().Replace("-", "_").Replace(" ", "_");
            return normalized == "AT_MATURITY" || normalized == "ATMATURITY";
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
            DateTime target = windowStart.AddMonths(months);
            int daysInMonth = DateTime.DaysInMonth(target.Year, target.Month);
            int day;
            bool isSourceEndOfMonth = windowStart.Day >= DateTime.DaysInMonth(windowStart.Year, windowStart.Month);
            if (isSourceEndOfMonth)
                day = daysInMonth;
            else
                day = Math.Min(windowStart.Day, daysInMonth);
            DateTime periodEnd = new DateTime(target.Year, target.Month, day);
            if (periodEnd.Day == 1)
                periodEnd = periodEnd.AddDays(-1);
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
                // BenchmarkRate is populated from Benchmark Master by the caller
                // or stored as a snapshot on FDInterest.
                // Effective Rate = Benchmark Rate + Margin
                return (interest.BenchmarkRate ?? 0m) + (interest.Margin ?? 0m);
            }
            return interest.InterestRate;
        }

        /// <summary>
        /// Resolves the benchmark rate using the rate history effective for the FD's start date.
        /// Falls back to Benchmark.CurrentRate if no history entry exists for the date.
        /// </summary>
        private async Task ResolveBenchmarkRateAsync(FDInterest interest, DateTime asOfDate)
        {
            if (interest.BenchmarkId.HasValue && interest.BenchmarkId.Value > 0)
            {
                var benchmark = await _interestRepository.GetBenchmarkByIdAsync(interest.BenchmarkId.Value);
                if (benchmark != null)
                {
                    interest.BenchmarkName = benchmark.BenchmarkName;
                    // Use the rate history effective for the FD's start date,
                    // NOT the benchmark's CurrentRate which may differ.
                    interest.BenchmarkRate = await _benchmarkRateHistoryService
                        .GetEffectiveRateAsync(interest.BenchmarkId.Value, asOfDate);
                }
            }
        }
    }
}