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
                    var existingCashFlows = await _cashFlowRepository.GetByFdIdAsync(fd.FdId);

                    foreach (var cf in existingCashFlows)
                    {
                        await _cashFlowRepository.DeleteAsync(cf.CashFlowId);
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
                var existingCashFlows = await _cashFlowRepository.GetByFdIdAsync(existingInterest.FdId);

                foreach (var cf in existingCashFlows)
                {
                    await _cashFlowRepository.DeleteAsync(cf.CashFlowId);
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

        public static string NormalizeFrequency(string? frequency)
        {
            if (string.IsNullOrWhiteSpace(frequency)) return string.Empty;

            var value = frequency.Trim().ToUpperInvariant().Replace("-", "_").Replace(" ", "_");

            if (value is "YEARLY" or "ANNUAL") return "ANNUALLY";
            if (value is "AT MATURITY") return "AT_MATURITY";
            if (value is "NOT APPLICABLE") return "NOT_APPLICABLE";
            if (value is "HALF YEARLY") return "HALF_YEARLY";

            return value;
        }

        private static void ValidateInterestConfiguration(FDInterest model)
        {
            model.InterestFrequency = NormalizeFrequency(model.InterestFrequency);
            model.CompoundingFrequency = NormalizeFrequency(model.CompoundingFrequency);

            ValidateInterestFrequency(model.InterestFrequency);

            if (model.IsCompounding)
            {
                if (string.IsNullOrWhiteSpace(model.CompoundingFrequency) ||
                    model.CompoundingFrequency == "NOT_APPLICABLE")
                {
                    throw new InvalidOperationException(
                        "Compounding Frequency is required when compounding is enabled.");
                }

                ValidateCompoundingFrequency(model.CompoundingFrequency);
            }
            else
            {
                model.CompoundingFrequency = "NOT_APPLICABLE";
            }
        }

        private static void ValidateInterestFrequency(string frequency)
        {
            if (string.IsNullOrWhiteSpace(frequency))
                throw new InvalidOperationException(
                    "Interest Frequency is required.");

            if (frequency is not
                ("MONTHLY" or
                 "QUARTERLY" or
                 "HALF_YEARLY" or
                 "ANNUALLY" or
                 "AT_MATURITY"))
            {
                throw new InvalidOperationException(
                    $"Unsupported Interest Frequency '{frequency}'.");
            }
        }

        private static void ValidateCompoundingFrequency(string frequency)
        {
            if (string.IsNullOrWhiteSpace(frequency))
                throw new InvalidOperationException(
                    "Compounding Frequency is required.");

            if (frequency is not
                ("MONTHLY" or
                 "QUARTERLY" or
                 "HALF_YEARLY" or
                 "ANNUALLY"))
            {
                throw new InvalidOperationException(
                    $"Unsupported Compounding Frequency '{frequency}'.");
            }
        }

        private static void AddScheduleDates(
            List<(DateTime Date, string Type)> events,
            DateTime startDate,
            DateTime endDate,
            string frequency,
            string eventType)
        {
            DateTime currentDate = startDate;

            while (true)
            {
                currentDate = GetNextScheduleDate(currentDate, frequency, startDate);

                if (currentDate > endDate)
                    break;

                events.Add((currentDate.Date, eventType));
            }
        }

        private static DateTime GetNextScheduleDate(
            DateTime currentDate,
            string frequency,
            DateTime originalStartDate)
        {
            if (string.IsNullOrWhiteSpace(frequency))
            {
                throw new InvalidOperationException("Frequency cannot be empty.");
            }

            bool isEom = originalStartDate.Day == DateTime.DaysInMonth(originalStartDate.Year, originalStartDate.Month);

            DateTime nextDate = frequency switch
            {
                "MONTHLY" => currentDate.AddMonths(1),
                "QUARTERLY" => currentDate.AddMonths(3),
                "HALF_YEARLY" => currentDate.AddMonths(6),
                "ANNUALLY" => currentDate.AddYears(1),
                "AT_MATURITY" => throw new InvalidOperationException("AT_MATURITY cannot be used to generate periodic dates."),
                "NOT_APPLICABLE" => throw new InvalidOperationException("NOT_APPLICABLE cannot be used to generate periodic dates."),
                _ => throw new InvalidOperationException($"Unsupported frequency '{frequency}'.")
            };

            if (isEom)
            {
                nextDate = new DateTime(nextDate.Year, nextDate.Month, DateTime.DaysInMonth(nextDate.Year, nextDate.Month));
            }

            return nextDate;
        }

        private List<FDCashFlow> GenerateCashFlows(
            FDIdentification fd,
            FDInterest interest)
        {
            var cashFlows = new List<FDCashFlow>();
            var now = DateTime.UtcNow;

            cashFlows.Add(new FDCashFlow
            {
                FdId = fd.FdId,
                Event = "FD Created",
                StartDate = fd.StartDate,
                EndDate = fd.StartDate,
                Days = 0,
                InterestRate = interest.InterestRate,
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

                if (isInterestDate)
                {
                    days = (date.Date - lastCalculationDate.Date).Days;
                    if (days > 0)
                    {
                        periodInterest = FinTrustFDManager.BAL.Common.FinancialCalculator.CalculateInterest(
                            balance, 
                            interest.InterestRate, 
                            days, 
                            interest.CalculationBasis);
                        
                        accruedInterest += periodInterest;
                    }
                }

                if (periodInterest == 0 && accruedInterest == 0)
                {
                    if (isInterestDate) lastCalculationDate = date;
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
                        InterestRate = interest.InterestRate,
                        OpeningBalance = balance,
                        InterestAmount = periodInterest,
                        ClosingBalance = balance,
                        CashFlowAmount = isCompounding ? 0 : periodInterest,
                        Direction = "INFLOW",
                        CurrencyCode = fd.CurrencyCode ?? "INR",
                        Status = "PENDING",
                        ReferenceNo = fd.FdReferenceNo ?? "",
                        CreatedDate = now
                    });
                }

                if (isCompoundingDate)
                {
                    decimal compoundedAmount = accruedInterest;
                    
                    cashFlows.Add(new FDCashFlow
                    {
                        FdId = fd.FdId,
                        Event = "Compounding Interest",
                        StartDate = date,
                        EndDate = date,
                        Days = 0,
                        InterestRate = interest.InterestRate,
                        OpeningBalance = balance,
                        InterestAmount = compoundedAmount,
                        ClosingBalance = balance + compoundedAmount,
                        CashFlowAmount = 0,
                        Direction = "INFLOW",
                        CurrencyCode = fd.CurrencyCode ?? "INR",
                        Status = "PENDING",
                        ReferenceNo = fd.FdReferenceNo ?? "",
                        CreatedDate = now
                    });
                    
                    balance += compoundedAmount;
                    accruedInterest = 0;
                }

                if (isInterestDate)
                {
                    lastCalculationDate = date;
                }
            }


            decimal maturityPeriodInterest = 0;
            if (lastCalculationDate < fd.EndDate)
            {
                int remainingDays = (fd.EndDate.Date - lastCalculationDate.Date).Days;
                if (remainingDays > 0)
                {
                    maturityPeriodInterest = FinTrustFDManager.BAL.Common.FinancialCalculator.CalculateInterest(
                        balance, 
                        interest.InterestRate, 
                        remainingDays, 
                        interest.CalculationBasis);

                    accruedInterest += maturityPeriodInterest;
                }
            }

            // -----------------------------------------------------------------
            // Emit final interest/compounding cashflow if any accrued interest remains
            // -----------------------------------------------------------------
            if (accruedInterest > 0)
            {
                int days = (fd.EndDate.Date - lastCalculationDate.Date).Days;
                
                if (days > 0)
                {
                    cashFlows.Add(new FDCashFlow
                    {
                        FdId = fd.FdId,
                        Event = "Interest",
                        StartDate = lastCalculationDate,
                        EndDate = fd.EndDate,
                        Days = days,
                        InterestRate = interest.InterestRate,
                        OpeningBalance = balance,
                        InterestAmount = maturityPeriodInterest,
                        ClosingBalance = balance,
                        CashFlowAmount = isCompounding ? 0 : maturityPeriodInterest,
                        Direction = "INFLOW",
                        CurrencyCode = fd.CurrencyCode ?? "INR",
                        Status = "PENDING",
                        ReferenceNo = fd.FdReferenceNo ?? "",
                        CreatedDate = now
                    });
                }

                if (isCompounding)
                {
                    cashFlows.Add(new FDCashFlow
                    {
                        FdId = fd.FdId,
                        Event = "Compounding Interest",
                        StartDate = fd.EndDate,
                        EndDate = fd.EndDate,
                        Days = 0,
                        InterestRate = interest.InterestRate,
                        OpeningBalance = balance,
                        InterestAmount = accruedInterest,
                        ClosingBalance = balance + accruedInterest,
                        CashFlowAmount = 0,
                        Direction = "INFLOW",
                        CurrencyCode = fd.CurrencyCode ?? "INR",
                        Status = "PENDING",
                        ReferenceNo = fd.FdReferenceNo ?? "",
                        CreatedDate = now
                    });
                    
                    balance += accruedInterest;
                }
                
                accruedInterest = 0;
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
                InterestRate = interest.InterestRate,
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

            return cashFlows;
        }
    }
}