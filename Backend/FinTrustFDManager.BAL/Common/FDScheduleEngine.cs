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

            string calcBasis = interest.DayCountConvention?.ConventionName
                ?? MapDayCountConventionId(interest.DayCountConventionId)
                ?? "ACTUAL_365";
            string interestFreq = interest.InterestFrequency?.FrequencyName
                ?? MapFrequencyId(interest.InterestFrequencyId)
                ?? "MONTHLY";
            bool isCompounding = interest.IsCompounding;
            string compFreq = interest.CompoundingFrequencyNavigation?.FrequencyName
                ?? MapFrequencyId(interest.CompoundingFrequencyId)
                ?? interestFreq;

            // P0-1 (BUG-001): the Angular form sends an EMPTY STRING (not null) for
            // PaymentConvention when the user never touched the field. Only treating
            // null as the default made every such FD silently defer all periodic
            // interest to maturity. Empty/whitespace now falls back to CASH as well.
            string paymentConv = string.IsNullOrWhiteSpace(interest.PaymentConvention)
                ? "CASH"
                : interest.PaymentConvention.Trim().ToUpperInvariant();

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
                AccruedInterest = 0m,
                CapitalizedInterest = 0m,
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

            // Start of the current compounding cycle. Compounding rows are generated
            // INDEPENDENTLY from the compounding-frequency calendar: each
            // "Compounding Interest" row spans from the start of its own cycle to the
            // cycle boundary (e.g. 02-Jan -> 02-Apr for a quarterly cycle), never just
            // the final interest sub-period (02-Mar -> 02-Apr).
            DateTime compoundingCycleStart = startDate;

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

                // ─────────────────────────────────────────────────────────────
                // Non-compounding maturity.
                // CASH convention + the final period being an interest period
                // (grid-aligned interest date, or AT_MATURITY where the whole
                // deposit is one interest period): the final interest is paid
                // out on its own "Interest" row and the Maturity row repays
                // the principal only.
                // Otherwise (MATURITY convention, or a partial final period that
                // is not an interest date): the final period is a single
                // "Maturity" row carrying its own interest — the existing
                // production behaviour, preserved exactly.
                // ─────────────────────────────────────────────────────────────
                if (isMaturity && !isCompounding)
                {
                    bool finalInterestPaidAsInterestRow = paymentConv == "CASH";

                    if (finalInterestPaidAsInterestRow)
                    {
                        cashFlows.Add(new FDCashFlow
                        {
                            FdId = fd.FdId,
                            Event = "Interest",
                            StartDate = periodStart,
                            EndDate = periodEnd,
                            Days = days,
                            InterestRate = effectiveRate,
                            OpeningBalance = balance,
                            InterestAmount = Math.Round(periodInterest, 2, MidpointRounding.AwayFromZero),
                            ClosingBalance = balance,
                            CashFlowAmount = Math.Round(accruedInterest, 2, MidpointRounding.AwayFromZero),
                            Direction = "INFLOW",
                            CurrencyCode = fd.CurrencyNavigation?.CurrencyCode ?? "INR",
                            Status = "PENDING",
                            ReferenceNo = fd.FdReferenceNo ?? "",
                            CreatedDate = now
                        });

                        cashFlows.Add(new FDCashFlow
                        {
                            FdId = fd.FdId,
                            Event = "Maturity",
                            StartDate = periodEnd,
                            EndDate = periodEnd,
                            Days = 0,
                            InterestRate = effectiveRate,
                            OpeningBalance = balance,
                            InterestAmount = 0m,
                            AccruedInterest = 0m,
                            CapitalizedInterest = 0m,
                            ClosingBalance = 0m,
                            CashFlowAmount = balance,
                            Direction = "INFLOW",
                            CurrencyCode = fd.CurrencyNavigation?.CurrencyCode ?? "INR",
                            Status = "PENDING",
                            ReferenceNo = fd.FdReferenceNo ?? "",
                            CreatedDate = now
                        });
                    }
                    else
                    {
                        cashFlows.Add(new FDCashFlow
                        {
                            FdId = fd.FdId,
                            Event = "Maturity",
                            StartDate = periodStart,
                            EndDate = periodEnd,
                            Days = days,
                            InterestRate = effectiveRate,
                            OpeningBalance = balance,
                            InterestAmount = Math.Round(periodInterest, 2, MidpointRounding.AwayFromZero),
                            AccruedInterest = 0m,
                            CapitalizedInterest = 0m,
                            ClosingBalance = 0m,
                            CashFlowAmount = Math.Round(balance + accruedInterest, 2, MidpointRounding.AwayFromZero),
                            Direction = "INFLOW",
                            CurrencyCode = fd.CurrencyNavigation?.CurrencyCode ?? "INR",
                            Status = "PENDING",
                            ReferenceNo = fd.FdReferenceNo ?? "",
                            CreatedDate = now
                        });
                    }

                    balance = 0m;
                    accruedInterest = 0m;
                    continue;
                }

                // ─────────────────────────────────────────────────────────────
                // Interest accrual detail row (period level, driven by the
                // user-selected INTEREST frequency). In compounding mode nothing
                // is paid in cash here; the accrual is capitalized by the
                // cycle-spanning "Compounding Interest" row below.
                // ─────────────────────────────────────────────────────────────
                if (isInterestDate || (isMaturity && isCompounding && intMonths.HasValue))
                {
                    bool paysCash = !isCompounding && paymentConv == "CASH";

                    cashFlows.Add(new FDCashFlow
                    {
                        FdId = fd.FdId,
                        Event = "Interest",
                        StartDate = periodStart,
                        EndDate = periodEnd,
                        Days = days,
                        InterestRate = effectiveRate,
                        OpeningBalance = balance,
                        InterestAmount = Math.Round(periodInterest, 2, MidpointRounding.AwayFromZero),
                        AccruedInterest = Math.Round(accruedInterest, 2, MidpointRounding.AwayFromZero),
                        CapitalizedInterest = 0m,
                        ClosingBalance = balance,
                        CashFlowAmount = paysCash ? Math.Round(accruedInterest, 2, MidpointRounding.AwayFromZero) : 0m,
                        Direction = paysCash ? "INFLOW" : "INTERNAL",
                        CurrencyCode = fd.CurrencyNavigation?.CurrencyCode ?? "INR",
                        Status = "PENDING",
                        ReferenceNo = fd.FdReferenceNo ?? "",
                        CreatedDate = now
                    });

                    if (paysCash)
                    {
                        accruedInterest = 0m;
                    }
                }

                // ─────────────────────────────────────────────────────────────
                // Compounding boundary: capitalize the WHOLE cycle.
                // FIX: StartDate is the start of the compounding cycle, Days is
                // the actual cycle length — both derived from the user-selected
                // compounding frequency. No hardcoded day counts.
                // ─────────────────────────────────────────────────────────────
                if (isCompoundingDate)
                {
                    balance = CapitalizeCycle(
                        cashFlows, fd, effectiveRate, calcBasis, now,
                        compoundingCycleStart, periodEnd, balance, accruedInterest);
                    accruedInterest = 0m;
                    compoundingCycleStart = periodEnd;
                }

                // ─────────────────────────────────────────────────────────────
                // Maturity inside a compounding schedule: the final cycle
                // (partial, or boundary-aligned) is captured as a Compounding
                // Interest row so no accrued interest is dropped and
                // Σ(Compounding InterestAmount) == Maturity − Principal.
                // The Maturity row then repays the final compounded balance.
                // ─────────────────────────────────────────────────────────────
                if (isMaturity && isCompounding)
                {
                    if (!isCompoundingDate)
                    {
                        balance = CapitalizeCycle(
                            cashFlows, fd, effectiveRate, calcBasis, now,
                            compoundingCycleStart, periodEnd, balance, accruedInterest);
                        accruedInterest = 0m;
                        compoundingCycleStart = periodEnd;
                    }

                    cashFlows.Add(new FDCashFlow
                    {
                        FdId = fd.FdId,
                        Event = "Maturity",
                        StartDate = periodEnd,
                        EndDate = periodEnd,
                        Days = 0,
                        InterestRate = effectiveRate,
                        OpeningBalance = balance,
                        InterestAmount = 0m,
                        AccruedInterest = 0m,
                        CapitalizedInterest = 0m,
                        ClosingBalance = 0m,
                        CashFlowAmount = balance,
                        Direction = "INFLOW",
                        CurrencyCode = fd.CurrencyNavigation?.CurrencyCode ?? "INR",
                        Status = "PENDING",
                        ReferenceNo = fd.FdReferenceNo ?? "",
                        CreatedDate = now
                    });

                    balance = 0m;
                }
            }

            // Normalise dates for Postgres
            foreach (var cf in cashFlows)
            {
                cf.StartDate = DateTime.SpecifyKind(cf.StartDate.Date, DateTimeKind.Utc);
                cf.EndDate = DateTime.SpecifyKind(cf.EndDate.Date, DateTimeKind.Utc);
            }

            return cashFlows;
        }

        /// <summary>
        /// Emits the cycle-spanning "Compounding Interest" row for the cycle
        /// [cycleStart, cycleEnd] and returns the new balance after capitalization.
        /// The capitalized amount is the rounded accumulated interest so the row
        /// satisfies OpeningBalance + InterestAmount == ClosingBalance exactly.
        /// </summary>
        private static decimal CapitalizeCycle(
            List<FDCashFlow> cashFlows,
            FDIdentification fd,
            decimal effectiveRate,
            string calcBasis,
            DateTime now,
            DateTime cycleStart,
            DateTime cycleEnd,
            decimal balance,
            decimal accruedInterest)
        {
            int cycleDays = FinancialCalculator.CalculateDays(cycleStart, cycleEnd, calcBasis);
            decimal capitalizedInterest = Math.Round(accruedInterest, 2, MidpointRounding.AwayFromZero);

            cashFlows.Add(new FDCashFlow
            {
                FdId = fd.FdId,
                Event = "Compounding Interest",
                StartDate = cycleStart,
                EndDate = cycleEnd,
                Days = cycleDays,
                InterestRate = effectiveRate,
                OpeningBalance = balance,
                InterestAmount = 0m,
                AccruedInterest = 0m,
                CapitalizedInterest = capitalizedInterest,
                ClosingBalance = balance + capitalizedInterest,
                CashFlowAmount = 0m,
                Direction = "INTERNAL",
                CurrencyCode = fd.CurrencyNavigation?.CurrencyCode ?? "INR",
                Status = "PENDING",
                ReferenceNo = fd.FdReferenceNo ?? "",
                CreatedDate = now
            });

            return balance + capitalizedInterest;
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

            if (normalized == "MONTHLY" || normalized == "MONTH") return 1;
            if (normalized == "QUARTERLY" || normalized == "QUARTER") return 3;
            if (normalized == "HALF_YEARLY" || normalized == "HALFYEARLY" || normalized == "SEMI_ANNUAL" || normalized == "SEMIANNUAL" || normalized == "SEMI_ANNUALLY" || normalized == "SEMIANNUALLY") return 6;
            if (normalized == "ANNUALLY" || normalized == "ANNUAL" || normalized == "YEARLY" || normalized == "YEAR") return 12;
            if (normalized == "AT_MATURITY" || normalized == "ATMATURITY") return null;

            // Dynamically parse "X Months" or "X_MONTHS"
            var parts = normalized.Split('_');
            if (parts.Length == 2 && (parts[1] == "MONTHS" || parts[1] == "MONTH"))
            {
                if (int.TryParse(parts[0], out int m))
                {
                    return m;
                }
            }
            return null;
        }

        private static string? MapFrequencyId(int? id) => id switch
        {
            1 => "Monthly",
            2 => "Quarterly",
            3 => "Half-Yearly",
            4 => "Annually",
            5 => "At Maturity",
            _ => null
        };

        private static string? MapDayCountConventionId(int? id) => id switch
        {
            1 => "30/360",
            2 => "Actual/360",
            3 => "Actual/365",
            4 => "Actual/Actual",
            _ => null
        };
    }
}
