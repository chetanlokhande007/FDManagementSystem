using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities.Investment;
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

        public FDInterestService(
            IFDInterestRepository interestRepository,
            IFDIdentificationRepository fdRepository,
            IFDCashFlowRepository cashFlowRepository,
            IUnitOfWork unitOfWork)
        {
            _interestRepository = interestRepository;
            _fdRepository = fdRepository;
            _cashFlowRepository = cashFlowRepository;
            _unitOfWork = unitOfWork;
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
            {
                throw new KeyNotFoundException(
                    $"FD with ID {model.FdId} not found.");
            }

            var existing = await _interestRepository.GetByFdIdAsync(model.FdId);

            if (existing != null)
            {
                throw new InvalidOperationException(
                    $"Interest already exists for FD ID {model.FdId}.");
            }

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
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<FDInterest?> UpdateAsync(
            long id,
            FDInterest model)
        {
            ValidateInterestConfiguration(model);

            var existingInterest = await _interestRepository.GetByIdAsync(id);

            if (existingInterest == null)
            {
                return null;
            }

            var fd = await _fdRepository.GetByIdAsync(existingInterest.FdId);

            if (fd == null)
            {
                throw new KeyNotFoundException(
                    $"FD with ID {existingInterest.FdId} not found.");
            }

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
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var existingInterest = await _interestRepository.GetByIdAsync(id);
            if (existingInterest == null)
            {
                return false;
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
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Frequency Normalization
        //  Maps any accepted frequency string (any casing, hyphens, aliases)
        //  to the number of months it represents.
        //  Returns null for "AT_MATURITY" (not a periodic frequency).
        // ═══════════════════════════════════════════════════════════════════

        private static int? GetFrequencyMonths(string frequency)
        {
            var normalized = frequency.Trim().ToUpperInvariant()
                .Replace("-", "_")
                .Replace(" ", "_");

            return normalized switch
            {
                "MONTHLY" or "MONTH" => 1,

                "QUARTERLY" or "QUARTER" => 3,

                "HALF_YEARLY" or "HALFYEARLY" or
                "SEMI_ANNUAL" or "SEMIANNUAL" or
                "SEMI_ANNUALLY" or "SEMIANNUALLY" or
                "SEMI_ANNUALS" => 6,

                "ANNUALLY" or "ANNUAL" or
                "YEARLY" or "YEAR" => 12,

                "AT_MATURITY" or "ATMATURITY" => null,

                _ => (int?)null  // Unknown — caller decides how to handle
            };
        }

        private static void ValidateInterestConfiguration(FDInterest model)
        {
            ValidateInterestFrequency(model.InterestFrequency);

            if (model.IsCompounding)
            {
                if (string.IsNullOrWhiteSpace(model.CompoundingFrequency) ||
                    model.CompoundingFrequency.Equals(
                        "Not Applicable",
                        StringComparison.OrdinalIgnoreCase) ||
                    model.CompoundingFrequency.Equals(
                        "NOT_APPLICABLE",
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Compounding Frequency is required when compounding is enabled.");
                }

                ValidateCompoundingFrequency(model.CompoundingFrequency);
            }
            else
            {
                model.CompoundingFrequency = "Not Applicable";
            }
        }

        private static void ValidateInterestFrequency(string? frequency)
        {
            if (string.IsNullOrWhiteSpace(frequency))
                throw new InvalidOperationException(
                    "Interest Frequency is required.");

            var months = GetFrequencyMonths(frequency);

            // "At Maturity" is a valid interest frequency but returns null months
            // because it's not a periodic frequency. Check using normalized form.
            var normalized = frequency.Trim().ToUpperInvariant()
                .Replace("-", "_").Replace(" ", "_");

            if (months == null && normalized != "AT_MATURITY")
            {
                throw new InvalidOperationException(
                    $"Unsupported Interest Frequency '{frequency}'.");
            }
        }

        private static void ValidateCompoundingFrequency(string? frequency)
        {
            if (string.IsNullOrWhiteSpace(frequency))
                throw new InvalidOperationException(
                    "Compounding Frequency is required.");

            var months = GetFrequencyMonths(frequency);

            // "At Maturity" is only valid for InterestFrequency, not CompoundingFrequency
            if (months == null)
            {
                throw new InvalidOperationException(
                    $"'{frequency}' is not a valid Compounding Frequency. Compounding requires a periodic frequency (Monthly, Quarterly, Half-Yearly, or Annually).\n" +
                    $"'At Maturity' is only valid as an Interest Frequency.");
            }
        }

        private static void AddScheduleDates(
            List<(DateTime Date, string Type)> events,
            DateTime startDate,
            DateTime endDate,
            string frequency,
            string eventType)
        {
            int? monthsToAdd = GetFrequencyMonths(frequency);

            if (monthsToAdd == null || monthsToAdd <= 0)
            {
                throw new InvalidOperationException($"Unsupported frequency '{frequency}'.");
            }

            // Anchor: the first schedule date is startDate + monthsToAdd
            DateTime nextDate = AddPeriodWithEomHandling(startDate, monthsToAdd.Value);

            while (nextDate <= endDate)
            {
                events.Add((nextDate.Date, eventType));
                nextDate = AddPeriodWithEomHandling(nextDate, monthsToAdd.Value);
            }
        }

        /// <summary>
        /// Adds a number of months to a date, preserving end-of-month semantics.
        /// If the source day is the last day of its month (e.g. 31 Jan, 28 Feb in non-leap),
        /// the result is the last day of the target month.
        /// Otherwise the same day-of-month is used, clamped to the target month's length.
        /// </summary>
        private static DateTime AddPeriodWithEomHandling(DateTime date, int months)
        {
            bool isLastDayOfMonth = date.Day == DateTime.DaysInMonth(date.Year, date.Month);

            DateTime result = date.AddMonths(months);

            if (isLastDayOfMonth)
            {
                // Snap to end of the target month
                int daysInTarget = DateTime.DaysInMonth(result.Year, result.Month);
                result = new DateTime(result.Year, result.Month, daysInTarget);
            }

            return result;
        }

        private static decimal GetEffectiveInterestRate(FDInterest interest)
        {
            if (string.Equals(interest.InterestRateType, "FLOATING", StringComparison.OrdinalIgnoreCase))
            {
                return (interest.BenchmarkRate ?? 0m) + (interest.Margin ?? 0m);
            }
            return interest.InterestRate;
        }

        private List<FDCashFlow> GenerateCashFlows(
            FDIdentification fd,
            FDInterest interest)
        {
            decimal effectiveRate = GetEffectiveInterestRate(interest);
            var cashFlows = new List<FDCashFlow>();
            var now = DateTime.UtcNow;

            cashFlows.Add(new FDCashFlow
            {
                FdId = fd.FdId,
                Event = "FD Created",
                StartDate = fd.StartDate,
                EndDate = fd.StartDate,
                Days = 0,
                InterestRate = effectiveRate,
                OpeningBalance = 0,
                InterestAmount = 0,
                ClosingBalance = fd.PrincipalAmount,
                CashFlowAmount = fd.PrincipalAmount,
                Direction = "OUTFLOW",
                CurrencyCode = fd.CurrencyCode ?? "INR",
                Status = "PENDING",
                ReferenceNo = fd.FdReferenceNo ?? "",
                CreatedDate = now
            });

            decimal balance = fd.PrincipalAmount;
            decimal accruedInterest = 0;
            DateTime lastCalculationDate = fd.StartDate;
            DateTime lastCompoundingDate = fd.StartDate;

            bool isCompounding = interest.IsCompounding;
            bool isAtMaturity = string.Equals(interest.InterestFrequency, "AT_MATURITY", StringComparison.OrdinalIgnoreCase);

            var events = new List<(DateTime Date, string Type)>();

            if (!isAtMaturity && !string.IsNullOrWhiteSpace(interest.InterestFrequency))
            {
                AddScheduleDates(events, fd.StartDate, fd.EndDate, interest.InterestFrequency, "Interest");
            }

            if (isCompounding && !string.IsNullOrWhiteSpace(interest.CompoundingFrequency))
            {
                AddScheduleDates(events, fd.StartDate, fd.EndDate, interest.CompoundingFrequency, "Compounding");
            }

            var sortedDates = events.Select(e => e.Date).Distinct().OrderBy(d => d).ToList();

            foreach (var date in sortedDates)
            {
                bool isCompoundingDate = events.Any(e => e.Date == date && e.Type == "Compounding") && isCompounding;
                bool isInterestDate = events.Any(e => e.Date == date && e.Type == "Interest");

                decimal periodInterest = 0;
                int days = 0;

                // Calculate interest for ANY event date (Interest or Compounding).
                // This handles AT_MATURITY + compounding where only Compounding events exist.
                if (isInterestDate || isCompoundingDate)
                {
                    days = (date.Date - lastCalculationDate.Date).Days;
                    if (days > 0)
                    {
                        periodInterest = FinTrustFDManager.BAL.Common.FinancialCalculator.CalculateInterest(
                            balance, 
                            effectiveRate, 
                            days, 
                            interest.CalculationBasis);
                        
                        accruedInterest += periodInterest;
                    }
                }

                if (periodInterest == 0 && accruedInterest == 0)
                {
                    if (isInterestDate || isCompoundingDate) lastCalculationDate = date;
                    if (isCompoundingDate) lastCompoundingDate = date;
                    continue;
                }

                if (isInterestDate)
                {
                    cashFlows.Add(new FDCashFlow
                    {
                        FdId = fd.FdId,
                        Event = "Interest",
                        StartDate = lastCalculationDate,
                        EndDate = date,
                        Days = days,
                        InterestRate = effectiveRate,
                        OpeningBalance = balance,
                        InterestAmount = Math.Round(periodInterest, 2),
                        ClosingBalance = balance,
                        CashFlowAmount = isCompounding ? 0 : Math.Round(periodInterest, 2),
                        Direction = "INFLOW",
                        CurrencyCode = fd.CurrencyCode ?? "INR",
                        Status = "PENDING",
                        ReferenceNo = fd.FdReferenceNo ?? "",
                        CreatedDate = now
                    });

                    // For non-compounding: interest is paid out each period, reset accrued.
                    // For compounding: keep accumulating until the compounding event.
                    if (!isCompounding)
                    {
                        accruedInterest = 0;
                    }
                }

                if (isCompoundingDate)
                {
                    decimal compoundedAmount = accruedInterest;
                    int compoundingDays = (date.Date - lastCompoundingDate.Date).Days;
                    
                    cashFlows.Add(new FDCashFlow
                    {
                        FdId = fd.FdId,
                        Event = "Compounding Interest",
                        StartDate = lastCompoundingDate,
                        EndDate = date,
                        Days = compoundingDays,
                        InterestRate = effectiveRate,
                        OpeningBalance = balance,
                        InterestAmount = Math.Round(compoundedAmount, 2),
                        ClosingBalance = balance + Math.Round(compoundedAmount, 2),
                        CashFlowAmount = 0,
                        Direction = "INFLOW",
                        CurrencyCode = fd.CurrencyCode ?? "INR",
                        Status = "PENDING",
                        ReferenceNo = fd.FdReferenceNo ?? "",
                        CreatedDate = now
                    });
                    
                    balance += Math.Round(compoundedAmount, 2);
                    accruedInterest = 0;
                    lastCompoundingDate = date;
                }

                if (isInterestDate || isCompoundingDate)
                {
                    lastCalculationDate = date;
                }
            }

            // -----------------------------------------------------------------
            // Partial period: interest from last schedule date to maturity
            // -----------------------------------------------------------------
            if (lastCalculationDate < fd.EndDate)
            {
                int days = (fd.EndDate.Date - lastCalculationDate.Date).Days;
                
                if (days > 0)
                {
                    // If compounding is enabled and there is un-compounded accrued
                    // interest from the last Interest event, compound it FIRST before
                    // calculating the partial period interest on the updated balance.
                    if (isCompounding && accruedInterest > 0)
                    {
                        int accruedDays = (lastCalculationDate.Date - lastCompoundingDate.Date).Days;
                        
                        cashFlows.Add(new FDCashFlow
                        {
                            FdId = fd.FdId,
                            Event = "Compounding Interest",
                            StartDate = lastCompoundingDate,
                            EndDate = lastCalculationDate,
                            Days = accruedDays,
                            InterestRate = effectiveRate,
                            OpeningBalance = balance,
                            InterestAmount = Math.Round(accruedInterest, 2),
                            ClosingBalance = balance + Math.Round(accruedInterest, 2),
                            CashFlowAmount = 0,
                            Direction = "INFLOW",
                            CurrencyCode = fd.CurrencyCode ?? "INR",
                            Status = "PENDING",
                            ReferenceNo = fd.FdReferenceNo ?? "",
                            CreatedDate = now
                        });
                        balance += Math.Round(accruedInterest, 2);
                        accruedInterest = 0;
                        lastCompoundingDate = lastCalculationDate;
                    }

                    decimal partialInterest = FinTrustFDManager.BAL.Common.FinancialCalculator.CalculateInterest(
                        balance, 
                        effectiveRate, 
                        days, 
                        interest.CalculationBasis);
                    
                    if (isCompounding)
                    {
                        // Compound the partial period interest into balance before maturity
                        cashFlows.Add(new FDCashFlow
                        {
                            FdId = fd.FdId,
                            Event = "Compounding Interest",
                            StartDate = lastCalculationDate,
                            EndDate = fd.EndDate,
                            Days = days,
                            InterestRate = effectiveRate,
                            OpeningBalance = balance,
                            InterestAmount = Math.Round(partialInterest, 2),
                            ClosingBalance = balance + Math.Round(partialInterest, 2),
                            CashFlowAmount = 0,
                            Direction = "INFLOW",
                            CurrencyCode = fd.CurrencyCode ?? "INR",
                            Status = "PENDING",
                            ReferenceNo = fd.FdReferenceNo ?? "",
                            CreatedDate = now
                        });
                        
                        balance += Math.Round(partialInterest, 2);
                    }
                    else
                    {
                        // Non-compounding: interest is paid out at maturity
                        cashFlows.Add(new FDCashFlow
                        {
                            FdId = fd.FdId,
                            Event = "Interest",
                            StartDate = lastCalculationDate,
                            EndDate = fd.EndDate,
                            Days = days,
                            InterestRate = effectiveRate,
                            OpeningBalance = balance,
                            InterestAmount = Math.Round(partialInterest, 2),
                            ClosingBalance = balance,
                            CashFlowAmount = Math.Round(partialInterest, 2),
                            Direction = "INFLOW",
                            CurrencyCode = fd.CurrencyCode ?? "INR",
                            Status = "PENDING",
                            ReferenceNo = fd.FdReferenceNo ?? "",
                            CreatedDate = now
                        });
                    }
                }
            }

            decimal maturityBalance = balance;

            // -----------------------------------------------------------------
            // Maturity
            // -----------------------------------------------------------------

            cashFlows.Add(new FDCashFlow
            {
                FdId = fd.FdId,
                Event = "Maturity",
                StartDate = fd.EndDate,
                EndDate = fd.EndDate,
                Days = 0,
                InterestRate = effectiveRate,
                OpeningBalance = maturityBalance,
                InterestAmount = 0,
                ClosingBalance = 0,
                CashFlowAmount = maturityBalance,
                Direction = "INFLOW",
                CurrencyCode = fd.CurrencyCode ?? "INR",
                Status = "PENDING",
                ReferenceNo = fd.FdReferenceNo ?? "",
                CreatedDate = now
            });

            // Ensure all DateTime fields are UTC for PostgreSQL timestamp with time zone
            foreach (var cf in cashFlows)
            {
                cf.StartDate = DateTime.SpecifyKind(cf.StartDate.Date, DateTimeKind.Utc);
                cf.EndDate = DateTime.SpecifyKind(cf.EndDate.Date, DateTimeKind.Utc);
                cf.CreatedDate = DateTime.UtcNow;
            }

            return cashFlows;
        }
    }
}