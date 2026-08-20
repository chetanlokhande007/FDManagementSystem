using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities.Investment;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FinTrustFDManager.BAL.Services
{
    public class FDInterestService(
        IFDInterestRepository interestRepository,
        IFDIdentificationRepository fdRepository,
        IFDCashFlowRepository cashFlowRepository) : IFDInterestService
    {
        private readonly IFDInterestRepository _interestRepository = interestRepository;
        private readonly IFDIdentificationRepository _fdRepository = fdRepository;
        private readonly IFDCashFlowRepository _cashFlowRepository = cashFlowRepository;

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

            if (!model.IsCompounding)
            {
                model.CompoundingFrequency = "Not Applicable";
            }

            model.CreatedDate = DateTime.UtcNow;

            var interest = await _interestRepository.AddAsync(model);

            var cashFlows = GenerateCashFlows(fd, interest);

            await _cashFlowRepository.AddRangeAsync(cashFlows);

            return interest;
        }

        /// <summary>
        /// Explicitly validates and resolves the day-count basis.
        /// Both "ACTUAL_360" and "ACTUAL_365" are checked directly (not via fallback),
        /// so an unexpected/typo'd value throws instead of silently defaulting to 365.
        /// </summary>
        private static decimal GetDayCountBasis(string? calculationBasis)
        {
            string basis = calculationBasis?.ToUpper()?.Trim() ?? "";

            if (basis == "ACTUAL_360")
            {
                return 360m;
            }
            else if (basis == "ACTUAL_365")
            {
                return 365m;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unsupported CalculationBasis '{calculationBasis}'. Expected 'ACTUAL_360' or 'ACTUAL_365'.");
            }
        }

        private List<FDCashFlow> GenerateCashFlows(
            FDIdentification fd,
            FDInterest interest)
        {
            var cashFlows = new List<FDCashFlow>();

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
                CreatedDate = DateTime.UtcNow
            });

            decimal openingBalance = fd.PrincipalAmount;
            DateTime previousDate = fd.StartDate;
            DateTime uncompoundedStartDate = fd.StartDate;
            decimal uncompoundedInterestSum = 0;

            // Generate dates for both interest payout and compounding
            var events = new List<(DateTime Date, string Type)>();

            // Add Interest Payout Dates
            if (!string.IsNullOrEmpty(interest.InterestFrequency) && interest.InterestFrequency.ToUpper() != "AT_MATURITY")
            {
                DateTime d = GetNextDate(fd.StartDate, interest.InterestFrequency);
                while (d <= fd.EndDate)
                {
                    events.Add((d, "Interest"));
                    d = GetNextDate(d, interest.InterestFrequency);
                }
            }

            // Add Compounding Dates
            if (interest.IsCompounding && !string.IsNullOrEmpty(interest.CompoundingFrequency))
            {
                DateTime d = GetNextDate(fd.StartDate, interest.CompoundingFrequency);
                while (d <= fd.EndDate)
                {
                    events.Add((d, "Compounding Interest"));
                    d = GetNextDate(d, interest.CompoundingFrequency);
                }
            }

            // Sort chronologically. If dates match, "Interest" comes before "Compounding Interest"
            var sortedEvents = events.OrderBy(e => e.Date).ThenBy(e => e.Type == "Interest" ? 0 : 1).ToList();

            // Process each event chronologically
            foreach (var ev in sortedEvents)
            {
                DateTime eventDate = ev.Date;
                string eventType = ev.Type;

                if (eventType == "Interest")
                {
                    int days = (eventDate.Date - previousDate.Date).Days;
                    decimal dayCountBasis = GetDayCountBasis(interest.CalculationBasis);

                    decimal calculatedInterest = openingBalance * (interest.InterestRate / 100m) * (days / dayCountBasis);
                    decimal roundedInterest = Math.Round(calculatedInterest, 2, MidpointRounding.AwayFromZero);

                    uncompoundedInterestSum += roundedInterest;

                    cashFlows.Add(new FDCashFlow
                    {
                        FdId = fd.FdId,
                        Event = eventType,
                        StartDate = previousDate,
                        EndDate = eventDate,
                        Days = days,
                        InterestRate = interest.InterestRate,
                        OpeningBalance = openingBalance,
                        InterestAmount = roundedInterest,
                        ClosingBalance = openingBalance, // Does not change principal
                        CashFlowAmount = roundedInterest,
                        Direction = "INFLOW",
                        CurrencyCode = fd.CurrencyCode ?? "INR",
                        Status = "PENDING",
                        ReferenceNo = fd.FdReferenceNo ?? "",
                        CreatedDate = DateTime.UtcNow
                    });

                    previousDate = eventDate;
                }
                else if (eventType == "Compounding Interest")
                {
                    int days = (eventDate.Date - previousDate.Date).Days;
                    decimal roundedInterest = 0;

                    if (days > 0)
                    {
                        decimal dayCountBasis = GetDayCountBasis(interest.CalculationBasis);
                        decimal calculatedInterest = openingBalance * (interest.InterestRate / 100m) * (days / dayCountBasis);
                        roundedInterest = Math.Round(calculatedInterest, 2, MidpointRounding.AwayFromZero);
                        uncompoundedInterestSum += roundedInterest;
                    }

                    decimal compoundedAmount = uncompoundedInterestSum;
                    uncompoundedInterestSum = 0; // Reset sum
                    decimal closingBalance = openingBalance + compoundedAmount;

                    cashFlows.Add(new FDCashFlow
                    {
                        FdId = fd.FdId,
                        Event = eventType,
                        StartDate = uncompoundedStartDate,
                        EndDate = eventDate,
                        Days = (eventDate.Date - uncompoundedStartDate.Date).Days,
                        InterestRate = interest.InterestRate,
                        OpeningBalance = openingBalance,
                        InterestAmount = compoundedAmount,
                        ClosingBalance = closingBalance,
                        CashFlowAmount = compoundedAmount,
                        Direction = "INFLOW",
                        CurrencyCode = fd.CurrencyCode ?? "INR",
                        Status = "PENDING",
                        ReferenceNo = fd.FdReferenceNo ?? "",
                        CreatedDate = DateTime.UtcNow
                    });

                    openingBalance = closingBalance;
                    previousDate = eventDate;
                    uncompoundedStartDate = eventDate;
                }
            }

            // Broken period logic
            if (previousDate < fd.EndDate)
            {
                int remainingDays = (fd.EndDate.Date - previousDate.Date).Days;
                if (remainingDays > 0)
                {
                    decimal dayCountBasis = GetDayCountBasis(interest.CalculationBasis);
                    decimal brokenPeriodInterest = openingBalance * (interest.InterestRate / 100m) * (remainingDays / dayCountBasis);
                    decimal finalInterest = Math.Round(brokenPeriodInterest, 2, MidpointRounding.AwayFromZero);
                    uncompoundedInterestSum += finalInterest;

                    cashFlows.Add(new FDCashFlow
                    {
                        FdId = fd.FdId,
                        Event = "Interest",
                        StartDate = previousDate,
                        EndDate = fd.EndDate,
                        Days = remainingDays,
                        InterestRate = interest.InterestRate,
                        OpeningBalance = openingBalance,
                        InterestAmount = finalInterest,
                        ClosingBalance = openingBalance,
                        CashFlowAmount = finalInterest,
                        Direction = "INFLOW",
                        CurrencyCode = fd.CurrencyCode ?? "INR",
                        Status = "PENDING",
                        ReferenceNo = fd.FdReferenceNo ?? "",
                        CreatedDate = DateTime.UtcNow
                    });
                }
            }

            // Maturity
            cashFlows.Add(new FDCashFlow
            {
                FdId = fd.FdId,
                Event = "Maturity",
                StartDate = fd.EndDate,
                EndDate = fd.EndDate,
                Days = 0,
                InterestRate = interest.InterestRate,
                OpeningBalance = openingBalance + uncompoundedInterestSum,
                InterestAmount = 0,
                ClosingBalance = 0,
                CashFlowAmount = openingBalance + uncompoundedInterestSum,
                Direction = "INFLOW",
                CurrencyCode = fd.CurrencyCode ?? "INR",
                Status = "PENDING",
                ReferenceNo = fd.FdReferenceNo ?? "",
                CreatedDate = DateTime.UtcNow
            });

            return cashFlows;
        }

        public async Task<FDInterest?> UpdateAsync(
            long id,
            FDInterest model)
        {
            if (!model.IsCompounding)
            {
                model.CompoundingFrequency = "Not Applicable";
            }

            model.FdInterestId = id;

            var updatedInterest = await _interestRepository.UpdateAsync(model);

            if (updatedInterest != null)
            {
                var fd = await _fdRepository.GetByIdAsync(updatedInterest.FdId);
                if (fd != null)
                {
                    // Get existing cashflows
                    var existingCashFlows = await _cashFlowRepository.GetByFdIdAsync(fd.FdId);

                    // Delete existing cashflows
                    foreach (var cf in existingCashFlows)
                    {
                        await _cashFlowRepository.DeleteAsync(cf.CashFlowId);
                    }

                    // Generate and save new cashflows
                    var newCashFlows = GenerateCashFlows(fd, updatedInterest);
                    await _cashFlowRepository.AddRangeAsync(newCashFlows);
                }
            }

            return updatedInterest;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            return await _interestRepository.DeleteAsync(id);
        }

        private static DateTime GetNextDate(DateTime currentDate, string frequency)
        {
            return (frequency?.ToUpper() ?? "") switch
            {
                "MONTHLY" => currentDate.AddMonths(1),
                "QUARTERLY" => currentDate.AddMonths(3),
                "HALF_YEARLY" => currentDate.AddMonths(6),
                "ANNUALLY" => currentDate.AddYears(1),
                _ => currentDate.AddMonths(3)
            };
        }
    }
}