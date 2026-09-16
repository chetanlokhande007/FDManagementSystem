using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using FinTrustFDManager.BAL.Common;
using FinTrustFDManager.Model.Entities;
using FinTrustFDManager.Model.Entities.Investment;
using FinTrustFDManager.Model.Entities.CoreData;

namespace FinTrustFDManager.BAL.Tests
{
    public class FDCompoundingTimelineTests
    {
        private static FDIdentification CreateFD(
            decimal principal,
            DateTime start,
            DateTime end,
            string refNo = "FD-TEST-001")
        {
            return new FDIdentification
            {
                FdId = 1,
                PrincipalAmount = principal,
                StartDate = start,
                EndDate = end,
                FdReferenceNo = refNo,
                CurrencyNavigation = new Currency { CurrencyCode = "INR" }
            };
        }

        private static FDInterest CreateInterest(
            decimal rate,
            string basis,
            string intFreq,
            bool isCompounding,
            string compFreq,
            string paymentConv)
        {
            return new FDInterest
            {
                InterestRateType = "FIXED",
                InterestRate = rate,
                DayCountConvention = new DayCountConvention { ConventionName = basis },
                InterestFrequency = new InterestFrequency { FrequencyName = intFreq },
                IsCompounding = isCompounding,
                CompoundingFrequencyNavigation = new InterestFrequency { FrequencyName = compFreq },
                PaymentConvention = paymentConv
            };
        }

        // ============================================================
        // PRIMARY REGRESSION CASE
        // Principal: ₹78,200 | Start: 01-Jan-2026 | Maturity: 31-Dec-2026
        // Rate: 5% | Interest: Monthly | Compounding: Half-Yearly
        // Apply Compounding: true | Day Count: Actual/365 | Payment: Maturity
        // ============================================================
        [Fact]
        public void PrimaryRegressionCase_MatchesExpected16EventSequence()
        {
            var fd = CreateFD(78_200m, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
            var interest = CreateInterest(5m, "Actual/365", "Monthly", true, "Half-Yearly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            // Exactly 16 events in order
            Assert.Equal(16, schedule.Count);

            // 1. FD Created 01-Jan-2026 → 01-Jan-2026
            Assert.Equal("FD Created", schedule[0].Event);
            Assert.Equal(new DateTime(2026, 1, 1), schedule[0].StartDate);
            Assert.Equal(new DateTime(2026, 1, 1), schedule[0].EndDate);
            Assert.Equal(0, schedule[0].Days);
            Assert.Equal(78_200m, schedule[0].ClosingBalance);

            // 2. Interest 01-Jan-2026 → 01-Feb-2026
            Assert.Equal("Interest", schedule[1].Event);
            Assert.Equal(new DateTime(2026, 1, 1), schedule[1].StartDate);
            Assert.Equal(new DateTime(2026, 2, 1), schedule[1].EndDate);
            Assert.Equal(31, schedule[1].Days);
            Assert.Equal(332.08m, schedule[1].InterestAmount);

            // 3. Interest 01-Feb-2026 → 01-Mar-2026
            Assert.Equal("Interest", schedule[2].Event);
            Assert.Equal(new DateTime(2026, 2, 1), schedule[2].StartDate);
            Assert.Equal(new DateTime(2026, 3, 1), schedule[2].EndDate);
            Assert.Equal(28, schedule[2].Days);
            Assert.Equal(299.95m, schedule[2].InterestAmount);

            // 4. Interest 01-Mar-2026 → 01-Apr-2026
            Assert.Equal("Interest", schedule[3].Event);
            Assert.Equal(new DateTime(2026, 3, 1), schedule[3].StartDate);
            Assert.Equal(new DateTime(2026, 4, 1), schedule[3].EndDate);
            Assert.Equal(31, schedule[3].Days);

            // 5. Interest 01-Apr-2026 → 01-May-2026
            Assert.Equal("Interest", schedule[4].Event);
            Assert.Equal(new DateTime(2026, 4, 1), schedule[4].StartDate);
            Assert.Equal(new DateTime(2026, 5, 1), schedule[4].EndDate);
            Assert.Equal(30, schedule[4].Days);

            // 6. Interest 01-May-2026 → 01-Jun-2026
            Assert.Equal("Interest", schedule[5].Event);
            Assert.Equal(new DateTime(2026, 5, 1), schedule[5].StartDate);
            Assert.Equal(new DateTime(2026, 6, 1), schedule[5].EndDate);
            Assert.Equal(31, schedule[5].Days);

            // 7. Interest 01-Jun-2026 → 01-Jul-2026
            Assert.Equal("Interest", schedule[6].Event);
            Assert.Equal(new DateTime(2026, 6, 1), schedule[6].StartDate);
            Assert.Equal(new DateTime(2026, 7, 1), schedule[6].EndDate);
            Assert.Equal(30, schedule[6].Days);

            // 8. Compounding Interest 01-Jan-2026 → 01-Jul-2026
            Assert.Equal("Compounding Interest", schedule[7].Event);
            Assert.Equal(new DateTime(2026, 1, 1), schedule[7].StartDate);
            Assert.Equal(new DateTime(2026, 7, 1), schedule[7].EndDate);
            Assert.Equal(181, schedule[7].Days);
            Assert.Equal(78_200m, schedule[7].OpeningBalance);
            Assert.Equal(1_938.93m, schedule[7].CapitalizedInterest);
            Assert.Equal(80_138.93m, schedule[7].ClosingBalance);
            Assert.Equal(1_938.93m, schedule[7].InterestAmount);
            Assert.Equal(1_938.93m, schedule[7].AccruedInterest);
            Assert.Equal(0m, schedule[7].CashFlowAmount);

            // 9. Interest 01-Jul-2026 → 01-Aug-2026 (starts at 80,138.93)
            Assert.Equal("Interest", schedule[8].Event);
            Assert.Equal(new DateTime(2026, 7, 1), schedule[8].StartDate);
            Assert.Equal(new DateTime(2026, 8, 1), schedule[8].EndDate);
            Assert.Equal(31, schedule[8].Days);
            Assert.Equal(80_138.93m, schedule[8].OpeningBalance);
            Assert.Equal(340.32m, schedule[8].InterestAmount);

            // 10. Interest 01-Aug-2026 → 01-Sep-2026
            Assert.Equal("Interest", schedule[9].Event);
            Assert.Equal(new DateTime(2026, 8, 1), schedule[9].StartDate);
            Assert.Equal(new DateTime(2026, 9, 1), schedule[9].EndDate);
            Assert.Equal(31, schedule[9].Days);

            // 11. Interest 01-Sep-2026 → 01-Oct-2026
            Assert.Equal("Interest", schedule[10].Event);
            Assert.Equal(new DateTime(2026, 9, 1), schedule[10].StartDate);
            Assert.Equal(new DateTime(2026, 10, 1), schedule[10].EndDate);
            Assert.Equal(30, schedule[10].Days);

            // 12. Interest 01-Oct-2026 → 01-Nov-2026
            Assert.Equal("Interest", schedule[11].Event);
            Assert.Equal(new DateTime(2026, 10, 1), schedule[11].StartDate);
            Assert.Equal(new DateTime(2026, 11, 1), schedule[11].EndDate);
            Assert.Equal(31, schedule[11].Days);

            // 13. Interest 01-Nov-2026 → 01-Dec-2026
            Assert.Equal("Interest", schedule[12].Event);
            Assert.Equal(new DateTime(2026, 11, 1), schedule[12].StartDate);
            Assert.Equal(new DateTime(2026, 12, 1), schedule[12].EndDate);
            Assert.Equal(30, schedule[12].Days);

            // 14. Interest 01-Dec-2026 → 31-Dec-2026
            Assert.Equal("Interest", schedule[13].Event);
            Assert.Equal(new DateTime(2026, 12, 1), schedule[13].StartDate);
            Assert.Equal(new DateTime(2026, 12, 31), schedule[13].EndDate);
            Assert.Equal(30, schedule[13].Days);

            // 15. Compounding Interest 01-Jul-2026 → 31-Dec-2026
            Assert.Equal("Compounding Interest", schedule[14].Event);
            Assert.Equal(new DateTime(2026, 7, 1), schedule[14].StartDate);
            Assert.Equal(new DateTime(2026, 12, 31), schedule[14].EndDate);
            Assert.Equal(183, schedule[14].Days);
            Assert.Equal(80_138.93m, schedule[14].OpeningBalance);
            Assert.Equal(2_008.96m, schedule[14].CapitalizedInterest);
            Assert.Equal(82_147.89m, schedule[14].ClosingBalance);
            Assert.Equal(2_008.96m, schedule[14].InterestAmount);
            Assert.Equal(2_008.96m, schedule[14].AccruedInterest);
            Assert.Equal(0m, schedule[14].CashFlowAmount);

            // 16. Maturity 31-Dec-2026 → 31-Dec-2026
            Assert.Equal("Maturity", schedule[15].Event);
            Assert.Equal(new DateTime(2026, 12, 31), schedule[15].StartDate);
            Assert.Equal(new DateTime(2026, 12, 31), schedule[15].EndDate);
            Assert.Equal(0, schedule[15].Days);
            Assert.Equal(82_147.89m, schedule[15].CashFlowAmount);
            Assert.Equal("INFLOW", schedule[15].Direction);

            Assert.All(schedule.Where(x => x.Event == "Compounding Interest"), x => Assert.True(x.Days > 0));
            Assert.All(schedule.Where(x => x.Event == "Compounding Interest"), x => Assert.True(x.StartDate < x.EndDate));

            // Interest continuity: continuous interest chain Jan 1 -> Dec 31
            var interestRows = schedule.Where(x => x.Event == "Interest").ToList();
            Assert.Equal(12, interestRows.Count);
            for (int i = 0; i < interestRows.Count - 1; i++)
            {
                Assert.Equal(interestRows[i].EndDate, interestRows[i + 1].StartDate);
            }
        }

        // TEST 1: Monthly interest + Half-Yearly compounding
        [Fact]
        public void Test1_MonthlyInterest_HalfYearlyCompounding_ContinuousTimeline()
        {
            var fd = CreateFD(50_000m, new DateTime(2026, 1, 1), new DateTime(2027, 1, 1));
            var interest = CreateInterest(6m, "Actual/365", "Monthly", true, "Half-Yearly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var compEvents = schedule.Where(x => x.Event == "Compounding Interest").ToList();
            Assert.Equal(2, compEvents.Count);
            Assert.Equal(new DateTime(2026, 1, 1), compEvents[0].StartDate);
            Assert.Equal(new DateTime(2026, 7, 1), compEvents[0].EndDate);
            Assert.Equal(181, compEvents[0].Days);

            Assert.Equal(new DateTime(2026, 7, 1), compEvents[1].StartDate);
            Assert.Equal(new DateTime(2027, 1, 1), compEvents[1].EndDate);
            Assert.Equal(184, compEvents[1].Days);
        }

        // TEST 2: Monthly interest + Quarterly compounding
        [Fact]
        public void Test2_MonthlyInterest_QuarterlyCompounding()
        {
            var fd = CreateFD(100_000m, new DateTime(2026, 1, 1), new DateTime(2026, 7, 1));
            var interest = CreateInterest(6m, "Actual/365", "Monthly", true, "Quarterly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var compEvents = schedule.Where(x => x.Event == "Compounding Interest").ToList();
            Assert.Equal(2, compEvents.Count);
            Assert.Equal(new DateTime(2026, 1, 1), compEvents[0].StartDate);
            Assert.Equal(new DateTime(2026, 4, 1), compEvents[0].EndDate);
            Assert.Equal(90, compEvents[0].Days);

            Assert.Equal(new DateTime(2026, 4, 1), compEvents[1].StartDate);
            Assert.Equal(new DateTime(2026, 7, 1), compEvents[1].EndDate);
            Assert.Equal(91, compEvents[1].Days);
        }

        // TEST 3: Monthly interest + Annual compounding
        [Fact]
        public void Test3_MonthlyInterest_AnnualCompounding()
        {
            var fd = CreateFD(100_000m, new DateTime(2026, 1, 1), new DateTime(2027, 1, 1));
            var interest = CreateInterest(6m, "Actual/365", "Monthly", true, "Annually", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var compEvents = schedule.Where(x => x.Event == "Compounding Interest").ToList();
            Assert.Single(compEvents);
            Assert.Equal(new DateTime(2026, 1, 1), compEvents[0].StartDate);
            Assert.Equal(new DateTime(2027, 1, 1), compEvents[0].EndDate);
            Assert.Equal(365, compEvents[0].Days);
        }

        // TEST 4: Compounding date exactly equals interest period end date
        [Fact]
        public void Test4_CompoundingDateEqualsInterestPeriodEndDate()
        {
            var fd = CreateFD(100_000m, new DateTime(2026, 1, 1), new DateTime(2026, 4, 1));
            var interest = CreateInterest(6m, "Actual/365", "Quarterly", true, "Quarterly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var intEvent = schedule.Single(x => x.Event == "Interest");
            var compEvent = schedule.Single(x => x.Event == "Compounding Interest");

            Assert.Equal(intEvent.StartDate, compEvent.StartDate);
            Assert.Equal(intEvent.EndDate, compEvent.EndDate);
            Assert.Equal(intEvent.Days, compEvent.Days);
        }

        // TEST 5: Maturity date exactly equals compounding date (No duplicate compounding)
        [Fact]
        public void Test5_MaturityDateEqualsCompoundingDate_NoDuplicate()
        {
            var fd = CreateFD(100_000m, new DateTime(2026, 1, 1), new DateTime(2026, 7, 1));
            var interest = CreateInterest(6m, "Actual/365", "Monthly", true, "Half-Yearly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var compAtMaturity = schedule.Where(x => x.Event == "Compounding Interest" && x.EndDate == new DateTime(2026, 7, 1)).ToList();
            Assert.Single(compAtMaturity);
            Assert.Equal(181, compAtMaturity[0].Days);
            Assert.Equal(new DateTime(2026, 1, 1), compAtMaturity[0].StartDate);

            var maturity = schedule.Single(x => x.Event == "Maturity");
            Assert.Equal(compAtMaturity[0].ClosingBalance, maturity.CashFlowAmount);
        }

        // TEST 6: Maturity date does NOT equal compounding date
        [Fact]
        public void Test6_MaturityDateDoesNotEqualCompoundingDate_PartialCycleCapitalized()
        {
            // Quarterly compounding (boundary Apr 1), but maturity is Mar 15
            var fd = CreateFD(100_000m, new DateTime(2026, 1, 1), new DateTime(2026, 3, 15));
            var interest = CreateInterest(6m, "Actual/365", "Monthly", true, "Quarterly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var compEvents = schedule.Where(x => x.Event == "Compounding Interest").ToList();
            Assert.Single(compEvents);
            Assert.Equal(new DateTime(2026, 1, 1), compEvents[0].StartDate);
            Assert.Equal(new DateTime(2026, 3, 15), compEvents[0].EndDate);
            Assert.Equal(73, compEvents[0].Days);

            var maturity = schedule.Single(x => x.Event == "Maturity");
            Assert.Equal(compEvents[0].ClosingBalance, maturity.CashFlowAmount);
        }

        // TEST 7: Non-month-end maturity date
        [Fact]
        public void Test7_NonMonthEndMaturityDate()
        {
            var fd = CreateFD(50_000m, new DateTime(2026, 1, 10), new DateTime(2026, 4, 25));
            var interest = CreateInterest(5m, "Actual/365", "Monthly", true, "Quarterly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var maturity = schedule.Single(x => x.Event == "Maturity");
            Assert.Equal(new DateTime(2026, 4, 25), maturity.StartDate);
            Assert.Equal(new DateTime(2026, 4, 25), maturity.EndDate);
            Assert.True(maturity.CashFlowAmount > 50_000m);
        }

        // TEST 8: Leap year
        [Fact]
        public void Test8_LeapYear_Feb29Counted()
        {
            var fd = CreateFD(100_000m, new DateTime(2028, 1, 1), new DateTime(2028, 4, 1));
            var interest = CreateInterest(6m, "Actual/365", "Monthly", true, "Quarterly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var febInterest = schedule.Single(x => x.Event == "Interest" && x.StartDate == new DateTime(2028, 2, 1));
            Assert.Equal(29, febInterest.Days);

            var compEvent = schedule.Single(x => x.Event == "Compounding Interest");
            Assert.Equal(new DateTime(2028, 1, 1), compEvent.StartDate);
            Assert.Equal(new DateTime(2028, 4, 1), compEvent.EndDate);
            Assert.Equal(91, compEvent.Days);
        }

        // TEST 9: Different month lengths
        [Fact]
        public void Test9_DifferentMonthLengths_ContinuousAccrual()
        {
            var fd = CreateFD(100_000m, new DateTime(2026, 1, 1), new DateTime(2026, 5, 1));
            var interest = CreateInterest(6m, "Actual/365", "Monthly", true, "Quarterly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var interestRows = schedule.Where(x => x.Event == "Interest").ToList();
            Assert.Equal(4, interestRows.Count);
            Assert.Equal(31, interestRows[0].Days); // Jan
            Assert.Equal(28, interestRows[1].Days); // Feb
            Assert.Equal(31, interestRows[2].Days); // Mar
            Assert.Equal(30, interestRows[3].Days); // Apr
        }

        // TEST 10: Apply Compounding Interest = false
        [Fact]
        public void Test10_ApplyCompoundingFalse_NoCompoundingEvents()
        {
            var fd = CreateFD(78_200m, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
            var interest = CreateInterest(5m, "Actual/365", "Monthly", false, "Half-Yearly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            Assert.DoesNotContain(schedule, x => x.Event == "Compounding Interest");
            Assert.All(schedule.Where(x => x.Event == "Interest"), row =>
            {
                Assert.Equal(78_200m, row.OpeningBalance);
                Assert.Equal(78_200m, row.ClosingBalance);
            });
        }

        // TEST 11: Payment convention = Maturity
        [Fact]
        public void Test11_PaymentConventionMaturity_CashFlowOnlyAtMaturity()
        {
            var fd = CreateFD(78_200m, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
            var interest = CreateInterest(5m, "Actual/365", "Monthly", true, "Half-Yearly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var periodicInterests = schedule.Where(x => x.Event == "Interest").ToList();
            Assert.All(periodicInterests, x =>
            {
                Assert.Equal(0m, x.CashFlowAmount);
                Assert.Equal("INTERNAL", x.Direction);
            });

            var maturity = schedule.Single(x => x.Event == "Maturity");
            Assert.True(maturity.CashFlowAmount > 78_200m);
            Assert.Equal("INFLOW", maturity.Direction);
        }

        // TEST 12: Prevent duplicate compounding events
        [Fact]
        public void Test12_PreventDuplicateCompoundingEvents()
        {
            var fd = CreateFD(100_000m, new DateTime(2026, 1, 1), new DateTime(2026, 7, 1));
            var interest = CreateInterest(6m, "Actual/365", "Monthly", true, "Half-Yearly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var compEvents = schedule.Where(x => x.Event == "Compounding Interest").ToList();
            Assert.Single(compEvents); // exactly 1 compounding event at 01-Jul
        }

        // TEST 13: Multiple compounding cycles
        [Fact]
        public void Test13_MultipleCompoundingCycles_BalanceProgression()
        {
            var fd = CreateFD(100_000m, new DateTime(2025, 1, 1), new DateTime(2027, 1, 1));
            var interest = CreateInterest(10m, "Actual/365", "Monthly", true, "Half-Yearly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var compEvents = schedule.Where(x => x.Event == "Compounding Interest").ToList();
            Assert.Equal(4, compEvents.Count);

            decimal currentBal = 100_000m;
            foreach (var ce in compEvents)
            {
                Assert.Equal(currentBal, ce.OpeningBalance);
                Assert.True(ce.CapitalizedInterest > 0);
                Assert.Equal(ce.OpeningBalance + ce.CapitalizedInterest, ce.ClosingBalance);
                currentBal = ce.ClosingBalance;
            }

            var maturity = schedule.Single(x => x.Event == "Maturity");
            Assert.Equal(currentBal, maturity.CashFlowAmount);
        }

        // TEST 14: Verify opening/closing balance continuity
        [Fact]
        public void Test14_OpeningClosingBalanceContinuity()
        {
            var fd = CreateFD(78_200m, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
            var interest = CreateInterest(5m, "Actual/365", "Monthly", true, "Half-Yearly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            // Row 7 (Interest Jun -> Jul) closing balance: 78,200
            Assert.Equal(78_200m, schedule[6].ClosingBalance);
            // Row 8 (Compounding Jul) opening: 78,200, closing: 80,138.93
            Assert.Equal(78_200m, schedule[7].OpeningBalance);
            Assert.Equal(80_138.93m, schedule[7].ClosingBalance);
            // Row 9 (Interest Jul -> Aug) opening: 80,138.93, closing: 80,138.93
            Assert.Equal(80_138.93m, schedule[8].OpeningBalance);
            Assert.Equal(80_138.93m, schedule[8].ClosingBalance);
            // Row 14 (Interest Dec -> Dec 31) closing: 80,138.93
            Assert.Equal(80_138.93m, schedule[13].ClosingBalance);
            // Row 15 (Compounding Dec 31) opening: 80,138.93, closing: 82,147.89
            Assert.Equal(80_138.93m, schedule[14].OpeningBalance);
            Assert.Equal(82_147.89m, schedule[14].ClosingBalance);
            // Row 16 (Maturity) opening: 82,147.89, cash flow: 82,147.89
            Assert.Equal(82_147.89m, schedule[15].OpeningBalance);
            Assert.Equal(82_147.89m, schedule[15].CashFlowAmount);
        }

        // TEST 15: Verify total interest is not double-counted
        [Fact]
        public void Test15_TotalInterestNotDoubleCounted()
        {
            var fd = CreateFD(78_200m, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
            var interest = CreateInterest(5m, "Actual/365", "Monthly", true, "Half-Yearly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            // In our architecture, unique economic interest is stored in InterestAmount on Interest rows.
            // Compounding rows have InterestAmount == 0.
            decimal sumInterestAmounts = schedule.Where(x => x.Event == "Interest").Sum(x => x.InterestAmount);
            decimal sumCapitalized = schedule.Where(x => x.Event == "Compounding Interest").Sum(x => x.CapitalizedInterest);

            // Sum of unique interest amounts matches sum of capitalized amounts within 2-cent rounding tolerance
            Assert.True(Math.Abs(sumCapitalized - Math.Round(sumInterestAmounts, 2)) <= 0.02m);

            // Maturity - Principal equals sum of capitalized amounts exactly
            var maturity = schedule.Single(x => x.Event == "Maturity");
            decimal totalEarned = maturity.CashFlowAmount - fd.PrincipalAmount;
            Assert.Equal(totalEarned, sumCapitalized);

            // Compounding rows show accurate aggregated interest amount
            Assert.All(schedule.Where(x => x.Event == "Compounding Interest"), x => Assert.True(x.InterestAmount > 0m));
        }

        // TEST 16: Monthly interest + Monthly compounding
        [Fact]
        public void Test16_MonthlyInterest_MonthlyCompounding()
        {
            var fd = CreateFD(60_000m, new DateTime(2026, 1, 1), new DateTime(2026, 4, 1));
            var interest = CreateInterest(6m, "Actual/365", "Monthly", true, "Monthly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var compEvents = schedule.Where(x => x.Event == "Compounding Interest").ToList();
            // Monthly compounding on Monthly interest: 3 compounding events (Jan, Feb, Mar)
            Assert.Equal(3, compEvents.Count);

            // Every compounding event spans its month
            Assert.Equal(new DateTime(2026, 1, 1), compEvents[0].StartDate);
            Assert.Equal(new DateTime(2026, 2, 1), compEvents[0].EndDate);
            Assert.Equal(31, compEvents[0].Days);

            Assert.Equal(new DateTime(2026, 2, 1), compEvents[1].StartDate);
            Assert.Equal(new DateTime(2026, 3, 1), compEvents[1].EndDate);
            Assert.Equal(28, compEvents[1].Days);

            Assert.Equal(new DateTime(2026, 3, 1), compEvents[2].StartDate);
            Assert.Equal(new DateTime(2026, 4, 1), compEvents[2].EndDate);
            Assert.Equal(31, compEvents[2].Days);

            foreach (var ce in compEvents)
            {
                Assert.True(ce.InterestAmount > 0m);
                Assert.Equal(ce.InterestAmount, ce.CapitalizedInterest);
                Assert.Equal("INTERNAL", ce.Direction);
            }

            // Balance must compound: each cycle's opening = previous cycle's closing
            decimal prevBalance = 60_000m;
            foreach (var ce in compEvents)
            {
                Assert.Equal(prevBalance, ce.OpeningBalance);
                Assert.Equal(ce.OpeningBalance + ce.CapitalizedInterest, ce.ClosingBalance);
                prevBalance = ce.ClosingBalance;
            }

            // Maturity pays the final compounded balance
            var maturity = schedule.Single(x => x.Event == "Maturity");
            Assert.Equal(prevBalance, maturity.CashFlowAmount);
        }

        // TEST 17: TotalInterest-equals-Maturity-minus-Principal at engine level
        // Validates that Sum(CapitalizedInterest) == MaturityAmount - Principal.
        // This is the invariant the service layer must honour.
        [Fact]
        public void Test17_SumCapitalizedInterest_EqualsMaturityMinusPrincipal()
        {
            var fd = CreateFD(45_200m, new DateTime(2026, 9, 3), new DateTime(2027, 11, 24));
            var interest = CreateInterest(5m, "Actual/365", "Monthly", true, "Half-Yearly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var maturity   = schedule.Single(x => x.Event == "Maturity");
            decimal sumCap = schedule
                .Where(x => x.Event == "Compounding Interest")
                .Sum(x => x.CapitalizedInterest);

            // Invariant: Sum(CapitalizedInterest) == MaturityAmount - Principal exactly
            Assert.Equal(maturity.CashFlowAmount - 45_200m, sumCap);
        }

        // TEST 18: Same-date event ordering (Interest → Compounding Interest → Maturity)
        [Fact]
        public void Test18_SameDateEvents_OrderedByPriority()
        {
            // Quarterly interest + quarterly compounding, maturity on compounding boundary
            var fd = CreateFD(100_000m, new DateTime(2026, 1, 1), new DateTime(2026, 4, 1));
            var interest = CreateInterest(6m, "Actual/365", "Quarterly", true, "Quarterly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            // All three events land on 01-Apr-2026
            var events = schedule.Where(x => x.EndDate == new DateTime(2026, 4, 1)).ToList();
            Assert.True(events.Count >= 3);

            int interestIdx   = schedule.IndexOf(schedule.First(x => x.Event == "Interest"     && x.EndDate == new DateTime(2026, 4, 1)));
            int compoundIdx   = schedule.IndexOf(schedule.First(x => x.Event == "Compounding Interest" && x.EndDate == new DateTime(2026, 4, 1)));
            int maturityIdx   = schedule.IndexOf(schedule.First(x => x.Event == "Maturity"));

            Assert.True(interestIdx < compoundIdx, "Interest must precede Compounding Interest on shared date");
            Assert.True(compoundIdx < maturityIdx,  "Compounding Interest must precede Maturity on shared date");
        }

        // TEST 19: Compounding validation for all compounding events spanning periods
        [Fact]
        public void Test19_AllCompoundingEvents_AreZeroDay()
        {
            var fd = CreateFD(100_000m, new DateTime(2025, 1, 1), new DateTime(2027, 1, 1));
            var interest = CreateInterest(8m, "Actual/365", "Monthly", true, "Quarterly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var compEvents = schedule.Where(x => x.Event == "Compounding Interest").ToList();
            Assert.True(compEvents.Count > 0, "Expected at least one Compounding Interest event");

            foreach (var ce in compEvents)
            {
                Assert.True(ce.StartDate < ce.EndDate);
                Assert.True(ce.Days > 0);
                // Compounding shows aggregated interest
                Assert.True(ce.InterestAmount > 0m);
                Assert.Equal(ce.CapitalizedInterest, ce.InterestAmount);
                Assert.Equal(ce.CapitalizedInterest, ce.AccruedInterest);
                Assert.Equal(0m, ce.CashFlowAmount);
                Assert.Equal("INTERNAL", ce.Direction);
                // Balance chain must hold
                Assert.Equal(ce.OpeningBalance + ce.CapitalizedInterest, ce.ClosingBalance);
            }

            // All compounding events must span their periods
            Assert.All(compEvents, x => Assert.True(x.Days > 0 && x.StartDate < x.EndDate));
        }

        // TEST 20: FD-0041 REAL-WORLD REGRESSION
        // Principal: ₹45,200 | Rate: 5% | Actual/365
        // Start: 03-Sep-2026 | Maturity: 24-Nov-2027 | Tenor: 447 days
        // Interest: Monthly | Compounding: Half-Yearly
        [Fact]
        public void Test20_FD0041_RegressionTest()
        {
            var fd = CreateFD(45_200m,
                new DateTime(2026, 9, 3),
                new DateTime(2027, 11, 24),
                "FD-0041");
            var interest = CreateInterest(5m, "Actual/365", "Monthly", true, "Half-Yearly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            // ── Compounding boundary dates ──────────────────────────────────────
            // H1 boundary: 03-Sep-2026 + 6 months = 03-Mar-2027
            // H2 boundary: 03-Sep-2026 + 12 months = 03-Sep-2027
            // Final (partial): maturity = 24-Nov-2027
            var compEvents = schedule.Where(x => x.Event == "Compounding Interest").ToList();
            Assert.Equal(3, compEvents.Count);

            // First compounding: 03-Sep-2026 → 03-Mar-2027, Days = 181
            Assert.Equal(new DateTime(2026, 9, 3), compEvents[0].StartDate);
            Assert.Equal(new DateTime(2027, 3, 3), compEvents[0].EndDate);
            Assert.Equal(181, compEvents[0].Days);

            // Second compounding: 03-Mar-2027 → 03-Sep-2027, Days = 184
            Assert.Equal(new DateTime(2027, 3, 3), compEvents[1].StartDate);
            Assert.Equal(new DateTime(2027, 9, 3), compEvents[1].EndDate);
            Assert.Equal(184, compEvents[1].Days);

            // Third (final/partial) compounding: 03-Sep-2027 → 24-Nov-2027, Days = 82
            Assert.Equal(new DateTime(2027, 9, 3), compEvents[2].StartDate);
            Assert.Equal(new DateTime(2027, 11, 24), compEvents[2].EndDate);
            Assert.Equal(82, compEvents[2].Days);

            // ── Invariant for all compounding events ───────────────────
            foreach (var ce in compEvents)
            {
                Assert.True(ce.StartDate < ce.EndDate);
                Assert.True(ce.Days > 0);
                Assert.True(ce.InterestAmount > 0m);
                Assert.Equal(ce.CapitalizedInterest, ce.InterestAmount);
            }

            // ── Final sequence (Interest → Compounding → Maturity) on 24-Nov-2027 ─
            var finalDayEvents = schedule
                .Where(x => x.EndDate == new DateTime(2027, 11, 24))
                .OrderBy(x => FDScheduleEngine.GetEventPriority(x.Event))
                .ToList();
            Assert.True(finalDayEvents.Count >= 3);
            Assert.Equal("Interest",             finalDayEvents[0].Event);
            Assert.Equal("Compounding Interest", finalDayEvents[1].Event);
            Assert.Equal("Maturity",             finalDayEvents[2].Event);

            // ── Balance continuity: expected progression ────────────────────────
            // Cycle 1 (H1 = 181 days: 03-Sep-2026 → 03-Mar-2027):
            //   45,200 × 0.05 × 181/365 ≈ 1,120.71 → ClosingBalance ≈ 46,320.71
            Assert.Equal(45_200m, compEvents[0].OpeningBalance);
            Assert.True(compEvents[0].CapitalizedInterest > 1_100m && compEvents[0].CapitalizedInterest < 1_150m,
                $"H1 capitalized interest ({compEvents[0].CapitalizedInterest}) expected ~1,120.71");
            decimal expectedH1Close = 45_200m + compEvents[0].CapitalizedInterest;
            Assert.Equal(expectedH1Close, compEvents[0].ClosingBalance);

            // Cycle 2 (H2 = 184 days: 03-Mar-2027 → 03-Sep-2027):
            Assert.Equal(expectedH1Close, compEvents[1].OpeningBalance);
            Assert.True(compEvents[1].CapitalizedInterest > 1_140m && compEvents[1].CapitalizedInterest < 1_200m,
                $"H2 capitalized interest ({compEvents[1].CapitalizedInterest}) expected ~1,167.54");
            decimal expectedH2Close = expectedH1Close + compEvents[1].CapitalizedInterest;
            Assert.Equal(expectedH2Close, compEvents[1].ClosingBalance);

            // Cycle 3 (partial: 03-Sep-2027 → 24-Nov-2027 = 82 days):
            Assert.Equal(expectedH2Close, compEvents[2].OpeningBalance);
            Assert.True(compEvents[2].CapitalizedInterest > 500m && compEvents[2].CapitalizedInterest < 570m,
                $"Partial capitalized interest ({compEvents[2].CapitalizedInterest}) expected ~533.43");
            decimal expectedFinalBalance = expectedH2Close + compEvents[2].CapitalizedInterest;
            Assert.Equal(expectedFinalBalance, compEvents[2].ClosingBalance);

            // ── Maturity settlement ────────────────────────────────────────────
            var maturity = schedule.Single(x => x.Event == "Maturity");
            Assert.Equal(expectedFinalBalance, maturity.CashFlowAmount);
            // Expected maturity payout: ≈ 48,021.68
            Assert.True(maturity.CashFlowAmount > 47_900m && maturity.CashFlowAmount < 48_100m,
                $"Maturity payout ({maturity.CashFlowAmount}) expected ~48,021.68");

            // ── Total interest = Sum(CapitalizedInterest) = Maturity - Principal ─
            decimal sumCap = compEvents.Sum(x => x.CapitalizedInterest);
            Assert.Equal(maturity.CashFlowAmount - 45_200m, sumCap);

            // ── No duplicate capitalization ────────────────────────────────────
            // Each compounding boundary must appear exactly once
            Assert.Equal(1, compEvents.Count(x => x.EndDate == new DateTime(2027, 3, 3)));
            Assert.Equal(1, compEvents.Count(x => x.EndDate == new DateTime(2027, 9, 3)));
            Assert.Equal(1, compEvents.Count(x => x.EndDate == new DateTime(2027, 11, 24)));

            // ── All compounding events span their periods ───────────────────────────
            Assert.All(compEvents, x => Assert.True(x.Days > 0 && x.StartDate < x.EndDate));
        }

        // Deterministic event priority check
        [Fact]
        public void EventPriority_IsDeterministic()
        {
            Assert.True(FDScheduleEngine.GetEventPriority("FD Created") < FDScheduleEngine.GetEventPriority("Interest"));
            Assert.True(FDScheduleEngine.GetEventPriority("Interest") < FDScheduleEngine.GetEventPriority("Compounding Interest"));
            Assert.True(FDScheduleEngine.GetEventPriority("Compounding Interest") < FDScheduleEngine.GetEventPriority("Maturity"));
        }
    }
}
