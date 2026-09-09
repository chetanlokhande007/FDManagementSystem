using FinTrustFDManager.Model.Entities.Investment;
using FinTrustFDManager.Model.Entities.CoreData;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FinTrustFDManager.BAL.Common
{
    public static class FDScheduleEngine
    {
        public static List<FDCashFlow> GenerateSchedule(FDIdentification fd, FDInterest interest)
        {
            var cashFlows = new List<FDCashFlow>();
            var now = DateTime.UtcNow;

            DateTime startDate = fd.StartDate.Date;
            DateTime maturityDate = fd.EndDate.Date;
            decimal principal = fd.PrincipalAmount;
            
            decimal effectiveRate = interest.InterestRateType?.ToUpper() == "FLOATING" 
                ? (interest.BenchmarkRate ?? 0m) + (interest.Margin ?? 0m) 
                : interest.InterestRate;

            string calcBasis = interest.DayCountConvention?.ConventionName ?? "ACTUAL_365";
            string interestFreq = interest.InterestFrequency?.FrequencyName ?? "MONTHLY";
            bool isCompounding = interest.IsCompounding;
            string compFreq = interest.CompoundingFrequencyNavigation?.FrequencyName ?? interestFreq;
            string paymentConv = interest.PaymentConvention?.ToUpper() ?? "CASH";

            // Determine frequency months
            int? intMonths = GetFrequencyMonths(interestFreq);
            int? compMonths = isCompounding ? GetFrequencyMonths(compFreq) : null;

            // Generate event dates
            var eventDates = new HashSet<DateTime> { startDate, maturityDate };

            if (intMonths.HasValue && intMonths.Value > 0)
            {
                int i = 1;
                DateTime dt = startDate.AddMonths(intMonths.Value * i);
                while (dt < maturityDate)
                {
                    eventDates.Add(dt);
                    i++;
                    dt = startDate.AddMonths(intMonths.Value * i);
                }
            }

            if (isCompounding && compMonths.HasValue && compMonths.Value > 0)
            {
                int i = 1;
                DateTime dt = startDate.AddMonths(compMonths.Value * i);
                while (dt < maturityDate)
                {
                    eventDates.Add(dt);
                    i++;
                    dt = startDate.AddMonths(compMonths.Value * i);
                }
            }

            var sortedDates = eventDates.OrderBy(d => d).ToList();

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
                ClosingBalance = principal,
                CashFlowAmount = principal,
                Direction = "OUTFLOW",
                CurrencyCode = fd.CurrencyNavigation?.CurrencyCode ?? "INR",
                Status = "PENDING",
                ReferenceNo = fd.FdReferenceNo ?? "",
                CreatedDate = now
            });

            decimal balance = principal;
            decimal accruedInterest = 0m;

            for (int i = 0; i < sortedDates.Count - 1; i++)
            {
                DateTime periodStart = sortedDates[i];
                DateTime periodEnd = sortedDates[i + 1];
                
                int days = FinancialCalculator.CalculateDays(periodStart, periodEnd, calcBasis);
                decimal periodInterest = FinancialCalculator.CalculateInterest(balance, effectiveRate, days, calcBasis);
                
                accruedInterest += periodInterest;

                bool isMaturity = periodEnd == maturityDate;
                bool isInterestDate = intMonths.HasValue && intMonths.Value > 0 && IsMultipleMonths(startDate, periodEnd, intMonths.Value);
                bool isCompoundingDate = isCompounding && compMonths.HasValue && compMonths.Value > 0 && IsMultipleMonths(startDate, periodEnd, compMonths.Value);

                decimal capitalizedInterest = 0m;
                decimal paidInterest = 0m;
                
                string eventType = "Interest";

                if (isCompoundingDate)
                {
                    capitalizedInterest = accruedInterest;
                    eventType = "Compounding Interest";
                }
                
                if (isInterestDate && paymentConv == "CASH" && !isCompoundingDate)
                {
                    paidInterest = accruedInterest;
                }

                if (isMaturity)
                {
                    eventType = "Maturity";
                    if (isCompounding)
                    {
                        capitalizedInterest = accruedInterest;
                    }
                    else
                    {
                        // Always payout remaining accrued interest at maturity
                        paidInterest = accruedInterest;
                    }
                }

                decimal newBalance = balance + capitalizedInterest;
                if (isMaturity)
                {
                    newBalance = 0m; // Maturity repays everything
                }

                decimal cashFlowAmount = paidInterest;
                if (isMaturity)
                {
                    cashFlowAmount += (balance + capitalizedInterest); // Repay principal + any final capitalized interest
                }

                cashFlows.Add(new FDCashFlow
                {
                    FdId = fd.FdId,
                    Event = eventType,
                    StartDate = periodStart,
                    EndDate = periodEnd,
                    Days = days,
                    InterestRate = effectiveRate,
                    OpeningBalance = balance,
                    InterestAmount = Math.Round(periodInterest, 2, MidpointRounding.AwayFromZero),
                    ClosingBalance = isMaturity ? 0m : Math.Round(newBalance, 2, MidpointRounding.AwayFromZero),
                    CashFlowAmount = Math.Round(cashFlowAmount, 2, MidpointRounding.AwayFromZero),
                    Direction = "INFLOW",
                    CurrencyCode = fd.CurrencyNavigation?.CurrencyCode ?? "INR",
                    Status = "PENDING",
                    ReferenceNo = fd.FdReferenceNo ?? "",
                    CreatedDate = now
                });

                if (capitalizedInterest > 0 || paidInterest > 0 || isMaturity)
                {
                    accruedInterest = 0m; // Reset after compounding or payment
                }
                
                balance = newBalance;
            }

            // Normalise dates for Postgres
            foreach (var cf in cashFlows)
            {
                cf.StartDate = DateTime.SpecifyKind(cf.StartDate.Date, DateTimeKind.Utc);
                cf.EndDate = DateTime.SpecifyKind(cf.EndDate.Date, DateTimeKind.Utc);
            }

            return cashFlows;
        }

        private static bool IsMultipleMonths(DateTime start, DateTime end, int months)
        {
            int monthDiff = (end.Year - start.Year) * 12 + end.Month - start.Month;
            if (monthDiff > 0 && monthDiff % months == 0 && start.AddMonths(monthDiff) == end)
                return true;
            return false;
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
    }
}
