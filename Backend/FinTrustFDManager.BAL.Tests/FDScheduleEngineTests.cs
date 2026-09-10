using System;
using System.Linq;
using Xunit;
using FinTrustFDManager.BAL.Common;
using FinTrustFDManager.Model.Entities.Investment;
using FinTrustFDManager.Model.Entities.CoreData;

namespace FinTrustFDManager.BAL.Tests
{
    public class FDScheduleEngineTests
    {
        private FDIdentification CreateFD(decimal principal, DateTime start, DateTime end)
        {
            return new FDIdentification
            {
                FdId = 1,
                PrincipalAmount = principal,
                StartDate = start,
                EndDate = end,
                FdReferenceNo = "TEST-FD-01"
            };
        }

        private FDInterest CreateInterest(decimal rate, string basis, string intFreq, bool isCompounding, string compFreq, string paymentConv)
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

        [Fact]
        public void Test1_NoCompounding_PrincipalNeverIncreases()
        {
            var fd = CreateFD(1000m, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
            var interest = CreateInterest(8m, "Actual/365", "Monthly", false, "Monthly", "CASH");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var periodicFlows = schedule.Where(x => x.Event == "Interest").ToList();
            Assert.All(periodicFlows, cf => Assert.Equal(1000m, cf.OpeningBalance));
            Assert.All(periodicFlows, cf => Assert.Equal(1000m, cf.ClosingBalance));
        }

        [Fact]
        public void Test2_MonthlyCompounding_PrincipalIncreasesEveryMonth()
        {
            var fd = CreateFD(1000m, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
            var interest = CreateInterest(8m, "Actual/365", "Monthly", true, "Monthly", "CAPITALIZE");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var compFlows = schedule.Where(x => x.Event == "Compounding Interest").ToList();
            // 11 monthly compounding events + 1 partial (Dec 1 → Dec 31): the final
            // partial cycle is captured as a Compounding Interest row so no accrued
            // interest is dropped (same contract as FD0094 / service-level tests).
            Assert.Equal(12, compFlows.Count);
            Assert.All(compFlows, cf => Assert.True(cf.ClosingBalance > cf.OpeningBalance));
            Assert.All(compFlows, cf => Assert.Equal(0m, cf.CashFlowAmount));
        }

        [Fact]
        public void Test3_SixMonthCompounding_MonthlyInterest()
        {
            var fd = CreateFD(1000m, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
            var interest = CreateInterest(8m, "Actual/365", "Monthly", true, "Half-Yearly", "CAPITALIZE");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var firstFiveFlows = schedule.Where(x => x.Event == "Interest" && x.EndDate < new DateTime(2026, 7, 1)).ToList();
            Assert.Equal(5, firstFiveFlows.Count);
            Assert.All(firstFiveFlows, cf => Assert.Equal(1000m, cf.ClosingBalance)); // No capitalization

            var monthSixFlow = schedule.FirstOrDefault(x => x.Event == "Compounding Interest" && x.EndDate == new DateTime(2026, 7, 1));
            Assert.NotNull(monthSixFlow);
            Assert.True(monthSixFlow.ClosingBalance > 1000m); // Capitalization happened!
        }

        [Fact]
        public void Test4_TwoMonthCompounding_CapitalizationBoundaries()
        {
            var fd = CreateFD(1000m, new DateTime(2026, 1, 1), new DateTime(2026, 7, 1));
            var interest = CreateInterest(8m, "Actual/365", "Monthly", true, "2 Months", "CAPITALIZE");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var compFlows = schedule.Where(x => x.Event == "Compounding Interest").ToList();
            // 2-month cycles: Jan→Mar, Mar→May, and May→Jul. The maturity date (Jul 1)
            // IS a compounding boundary, so the final full cycle is emitted as a
            // Compounding Interest row (no missing compounding event at a
            // boundary-aligned maturity) and the Maturity row repays the balance.
            Assert.Equal(3, compFlows.Count);
            Assert.Equal(new DateTime(2026, 3, 1), compFlows[0].EndDate);
            Assert.Equal(new DateTime(2026, 5, 1), compFlows[1].EndDate);
            Assert.Equal(new DateTime(2026, 7, 1), compFlows[2].EndDate);
            Assert.Equal(new DateTime(2026, 5, 1), compFlows[2].StartDate);
        }

        [Fact]
        public void Test5_CashInterest_CreatesActualInflow()
        {
            var fd = CreateFD(1000m, new DateTime(2026, 1, 1), new DateTime(2026, 4, 1));
            var interest = CreateInterest(8m, "Actual/365", "Monthly", false, "Monthly", "CASH");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var firstInterest = schedule.FirstOrDefault(x => x.Event == "Interest");
            Assert.NotNull(firstInterest);
            Assert.Equal(firstInterest.InterestAmount, firstInterest.CashFlowAmount); // Paid out entirely
            Assert.Equal("INFLOW", firstInterest.Direction);
            Assert.Equal(1000m, firstInterest.ClosingBalance);
        }

        [Fact]
        public void Test6_MaturityInterest_SettledOnlyAtMaturity()
        {
            var fd = CreateFD(1000m, new DateTime(2026, 1, 1), new DateTime(2026, 4, 1));
            var interest = CreateInterest(8m, "Actual/365", "Monthly", false, "Monthly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var periodicFlows = schedule.Where(x => x.Event == "Interest").ToList();
            Assert.All(periodicFlows, cf => Assert.Equal(0m, cf.CashFlowAmount)); // Nothing paid periodically

            var maturityFlow = schedule.FirstOrDefault(x => x.Event == "Maturity");
            Assert.NotNull(maturityFlow);
            Assert.True(maturityFlow.CashFlowAmount > 1000m); // Principal + 3 months accumulated interest
        }

        [Fact]
        public void Test7_NoDoubleCounting()
        {
            var principal = 1000m;
            var fd = CreateFD(principal, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
            var interest = CreateInterest(8m, "Actual/365", "Monthly", false, "Monthly", "CASH");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var totalInterestGen = schedule.Sum(x => x.InterestAmount);
            var totalCashFlows = schedule.Where(x => x.Direction == "INFLOW").Sum(x => x.CashFlowAmount);

            // Reconciles perfectly!
            Assert.Equal(Math.Round(principal + totalInterestGen, 2), totalCashFlows);
        }

        [Fact]
        public void Test8_PartialFinalPeriod_UsesCorrectDays()
        {
            var fd = CreateFD(1000m, new DateTime(2026, 9, 5), new DateTime(2026, 10, 15)); // Monthly freq
            var interest = CreateInterest(8m, "Actual/365", "Monthly", false, "Monthly", "CASH");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var partialFlow = schedule.Where(x => x.Event == "Interest").OrderBy(x => x.EndDate).Last();
            Assert.NotNull(partialFlow);
            Assert.Equal(10, partialFlow.Days); // Oct 5 to Oct 15
        }

        [Fact]
        public void Test9_BasisDifferences()
        {
            var fd = CreateFD(1000m, new DateTime(2026, 1, 1), new DateTime(2026, 2, 1));
            
            var int365 = CreateInterest(8m, "Actual/365", "At Maturity", false, "At Maturity", "MATURITY");
            var sch365 = FDScheduleEngine.GenerateSchedule(fd, int365);
            
            var int360 = CreateInterest(8m, "Actual/360", "At Maturity", false, "At Maturity", "MATURITY");
            var sch360 = FDScheduleEngine.GenerateSchedule(fd, int360);

            var flow365 = sch365.First(x => x.Event == "Maturity");
            var flow360 = sch360.First(x => x.Event == "Maturity");

            Assert.True(flow360.InterestAmount > flow365.InterestAmount); // 360 yields slightly higher interest
        }

        // ═════════════════════════════════════════════════════════════
        // Spec regression tests: compounding-period boundary correctness.
        // Compounding rows must span their own full cycle (derived from the
        // user-selected compounding frequency), never just the final interest
        // sub-period. All expected day counts are computed from actual DateTime
        // arithmetic — no hardcoded 90/91/92.
        // ═════════════════════════════════════════════════════════════

        private static FDCashFlow SingleCompounding(List<FDCashFlow> schedule, DateTime endDate)
            => schedule.Single(x => x.Event == "Compounding Interest" && x.EndDate == endDate);

        [Fact]
        public void Compounding_Quarterly_FirstCycle_SpansJanToApr()
        {
            // Spec critical case: Interest=Monthly, Compounding=Quarterly, start 02-Jan-2026
            var fd = CreateFD(63_200m, new DateTime(2026, 1, 2), new DateTime(2026, 7, 2));
            var interest = CreateInterest(3m, "Actual/365", "Monthly", true, "Quarterly", "CASH");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var comp1 = SingleCompounding(schedule, new DateTime(2026, 4, 2));
            Assert.Equal(new DateTime(2026, 1, 2), comp1.StartDate); // cycle start, NOT 02-Mar
            Assert.Equal((new DateTime(2026, 4, 2) - new DateTime(2026, 1, 2)).Days, comp1.Days); // 90
            Assert.Equal(90, comp1.Days);
        }

        [Fact]
        public void Compounding_Quarterly_SecondCycle_SpansAprToJul()
        {
            var fd = CreateFD(63_200m, new DateTime(2026, 1, 2), new DateTime(2026, 7, 2));
            var interest = CreateInterest(3m, "Actual/365", "Monthly", true, "Quarterly", "CASH");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var comp2 = SingleCompounding(schedule, new DateTime(2026, 7, 2));
            Assert.Equal(new DateTime(2026, 4, 2), comp2.StartDate); // continues from cycle 1 end
            Assert.Equal((new DateTime(2026, 7, 2) - new DateTime(2026, 4, 2)).Days, comp2.Days); // 91
            Assert.Equal(91, comp2.Days);
        }

        [Fact]
        public void Compounding_IndependentOfInterestFrequency_SubPeriodsKept()
        {
            // Interest rows stay at the MONTHLY cadence while compounding rows span QUARTERS.
            var fd = CreateFD(63_200m, new DateTime(2026, 1, 2), new DateTime(2026, 4, 2));
            var interest = CreateInterest(3m, "Actual/365", "Monthly", true, "Quarterly", "CASH");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var interestRows = schedule.Where(x => x.Event == "Interest").OrderBy(x => x.EndDate).ToList();
            Assert.Equal(3, interestRows.Count); // Jan→Feb, Feb→Mar, Mar→Apr
            Assert.Equal(new DateTime(2026, 2, 2), interestRows[0].EndDate);
            Assert.Equal(new DateTime(2026, 3, 2), interestRows[1].EndDate);
            Assert.Equal(new DateTime(2026, 4, 2), interestRows[2].EndDate);

            // Exactly one compounding row for the quarter, spanning the whole cycle.
            var compRows = schedule.Where(x => x.Event == "Compounding Interest").ToList();
            Assert.Single(compRows);
            Assert.Equal(new DateTime(2026, 1, 2), compRows[0].StartDate);
            Assert.Equal(new DateTime(2026, 4, 2), compRows[0].EndDate);
            Assert.NotEqual(new DateTime(2026, 3, 2), compRows[0].StartDate); // the old bug

            // Reconciliation invariant: Σ compounding interest == maturity − principal.
            var totalComp = compRows.Sum(x => x.CapitalizedInterest);
            var maturity = schedule.Single(x => x.Event == "Maturity");
            Assert.Equal(Math.Round(63_200m + totalComp, 2), maturity.CashFlowAmount);
        }

        [Fact]
        public void Compounding_HalfYearly_FirstCycle_SpansJanToJul()
        {
            var fd = CreateFD(100_000m, new DateTime(2026, 1, 2), new DateTime(2027, 1, 2));
            var interest = CreateInterest(3m, "Actual/365", "Quarterly", true, "Half-Yearly", "CAPITALIZE");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var comp1 = SingleCompounding(schedule, new DateTime(2026, 7, 2));
            Assert.Equal(new DateTime(2026, 1, 2), comp1.StartDate);
            Assert.Equal((new DateTime(2026, 7, 2) - new DateTime(2026, 1, 2)).Days, comp1.Days);
        }

        [Fact]
        public void Compounding_Annual_FirstCycle_SpansJanToJanNextYear()
        {
            var fd = CreateFD(100_000m, new DateTime(2026, 1, 2), new DateTime(2027, 2, 2));
            var interest = CreateInterest(3m, "Actual/365", "Monthly", true, "Annually", "CAPITALIZE");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var comp1 = SingleCompounding(schedule, new DateTime(2027, 1, 2));
            Assert.Equal(new DateTime(2026, 1, 2), comp1.StartDate);
            Assert.Equal((new DateTime(2027, 1, 2) - new DateTime(2026, 1, 2)).Days, comp1.Days); // 365
            Assert.Equal(365, comp1.Days);
        }

        [Fact]
        public void Compounding_MonthlyInterest_MonthlyCompounding_Aligned()
        {
            var fd = CreateFD(1_000m, new DateTime(2026, 1, 2), new DateTime(2026, 4, 2));
            var interest = CreateInterest(8m, "Actual/365", "Monthly", true, "Monthly", "CAPITALIZE");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var compRows = schedule.Where(x => x.Event == "Compounding Interest").OrderBy(x => x.EndDate).ToList();
            Assert.Equal(3, compRows.Count); // ends Feb 2, Mar 2, Apr 2
            Assert.Equal(new DateTime(2026, 2, 2), compRows[0].EndDate);
            Assert.Equal(new DateTime(2026, 3, 2), compRows[1].EndDate);
            Assert.Equal(new DateTime(2026, 4, 2), compRows[2].EndDate);

            // Each cycle starts where the previous one ended (no gaps, no overlaps).
            Assert.Equal(new DateTime(2026, 1, 2), compRows[0].StartDate);
            Assert.Equal(compRows[0].EndDate, compRows[1].StartDate);
            Assert.Equal(compRows[1].EndDate, compRows[2].StartDate);

            // Cycle day counts equal the actual month lengths (31, 28, 31).
            Assert.Equal(31, compRows[0].Days);
            Assert.Equal(28, compRows[1].Days);
            Assert.Equal(31, compRows[2].Days);
        }

        [Fact]
        public void Compounding_Disabled_NoCompoundingRows()
        {
            var fd = CreateFD(1_000m, new DateTime(2026, 1, 2), new DateTime(2026, 7, 2));
            var interest = CreateInterest(8m, "Actual/365", "Monthly", false, "Quarterly", "CASH");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            Assert.DoesNotContain(schedule, x => x.Event == "Compounding Interest");
        }

        [Fact]
        public void Compounding_MaturityBeforeBoundary_PartialCycleCaptured()
        {
            // Start 02-Jan, quarterly boundary 02-Apr, maturity 15-Mar:
            // the final partial cycle must be a Compounding row spanning 02-Jan→15-Mar.
            var fd = CreateFD(1_000m, new DateTime(2026, 1, 2), new DateTime(2026, 3, 15));
            var interest = CreateInterest(8m, "Actual/365", "Monthly", true, "Quarterly", "CAPITALIZE");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var compRows = schedule.Where(x => x.Event == "Compounding Interest").ToList();
            Assert.Single(compRows);
            Assert.Equal(new DateTime(2026, 1, 2), compRows[0].StartDate);
            Assert.Equal(new DateTime(2026, 3, 15), compRows[0].EndDate);
            Assert.Equal((new DateTime(2026, 3, 15) - new DateTime(2026, 1, 2)).Days, compRows[0].Days);
            Assert.All(compRows, cf => Assert.True(cf.Days > 0)); // no zero-length periods

            // Balance chain: maturity repays exactly the compounded balance.
            var maturity = schedule.Single(x => x.Event == "Maturity");
            Assert.Equal(compRows[0].ClosingBalance, maturity.CashFlowAmount);
        }

        [Fact]
        public void Compounding_MaturityExactlyOnBoundary_NoDuplicateOrMissing()
        {
            // Maturity 02-Apr IS the quarterly boundary: exactly one compounding row
            // must end there (no duplicate), and the Maturity row repays the balance.
            var fd = CreateFD(1_000m, new DateTime(2026, 1, 2), new DateTime(2026, 4, 2));
            var interest = CreateInterest(8m, "Actual/365", "Monthly", true, "Quarterly", "CAPITALIZE");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var compRowsAtMaturity = schedule.Where(x => x.Event == "Compounding Interest" && x.EndDate == new DateTime(2026, 4, 2)).ToList();
            Assert.Single(compRowsAtMaturity);
            Assert.Equal(new DateTime(2026, 1, 2), compRowsAtMaturity[0].StartDate); // full cycle, not just Mar→Apr

            var maturity = schedule.Single(x => x.Event == "Maturity");
            Assert.Equal(compRowsAtMaturity[0].ClosingBalance, maturity.CashFlowAmount);
        }

        [Fact]
        public void Compounding_LeapYear_Feb29CountedInDayCounts()
        {
            // Cycle Dec-2027 → Mar-2028 contains leap-day 29-Feb-2028.
            var fd = CreateFD(1_000m, new DateTime(2027, 12, 2), new DateTime(2028, 6, 2));
            var interest = CreateInterest(8m, "Actual/365", "Monthly", true, "Quarterly", "CAPITALIZE");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            // Monthly interest sub-period across February must have 29 days.
            var febRow = schedule.Single(x => x.Event == "Interest" && x.StartDate == new DateTime(2028, 2, 2));
            Assert.Equal((new DateTime(2028, 3, 2) - new DateTime(2028, 2, 2)).Days, febRow.Days);
            Assert.Equal(29, febRow.Days);

            // The quarterly compounding row spanning the leap February uses actual days too.
            var comp1 = SingleCompounding(schedule, new DateTime(2028, 3, 2));
            Assert.Equal((new DateTime(2028, 3, 2) - new DateTime(2027, 12, 2)).Days, comp1.Days);
            Assert.Equal(91, comp1.Days);
        }

        [Fact]
        public void PaymentConvention_EmptyString_TreatedAsCash_PeriodicInterestPaid()
        {
            // P0-1 / BUG-001 regression: the Angular form sends "" (not null).
            // Before the fix, "" fell through to the MATURITY branch and every
            // periodic Interest row paid 0.00 in cash.
            var fd = CreateFD(1_000m, new DateTime(2026, 1, 2), new DateTime(2026, 4, 2));
            var interest = CreateInterest(8m, "Actual/365", "Monthly", false, "Monthly", "");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var interestRows = schedule.Where(x => x.Event == "Interest").ToList();
            Assert.NotEmpty(interestRows);
            Assert.All(interestRows, cf => Assert.Equal(cf.InterestAmount, cf.CashFlowAmount));
            Assert.All(interestRows, cf => Assert.True(cf.CashFlowAmount > 0m));

            // Maturity repays principal only (interest was already paid in cash).
            var maturity = schedule.Single(x => x.Event == "Maturity");
            Assert.Equal(1_000m, maturity.CashFlowAmount);
        }

        [Fact]
        public void Test10_Thirty360_Mathematics()
        {
            var fd = CreateFD(85000m, new DateTime(2026, 1, 1), new DateTime(2026, 2, 1));
            var interest = CreateInterest(5m, "30/360", "At Maturity", false, "At Maturity", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var maturityRow = schedule.First(x => x.Event == "Maturity");
            Assert.Equal(30, maturityRow.Days); // Jan 1 to Feb 1 is EXACTLY 30 days in 30/360
            Assert.Equal(354.17m, maturityRow.InterestAmount); // 85000 * 0.05 * 30 / 360
        }

        [Fact]
        public void Test11_DifferentFrequencies()
        {
            var fd = CreateFD(1000m, new DateTime(2026, 1, 1), new DateTime(2026, 7, 1));
            
            // Config A: Monthly Interest, Quarterly Compounding
            var intA = CreateInterest(12m, "Actual/365", "Monthly", true, "Quarterly", "MATURITY");
            var schA = FDScheduleEngine.GenerateSchedule(fd, intA);
            
            // Config B: Monthly Interest, Monthly Compounding
            var intB = CreateInterest(12m, "Actual/365", "Monthly", true, "Monthly", "MATURITY");
            var schB = FDScheduleEngine.GenerateSchedule(fd, intB);

            // Month 1 (Feb 1): Config A does NOT capitalize. Config B DOES capitalize.
            var aFeb = schA.FirstOrDefault(x => x.EndDate == new DateTime(2026, 2, 1) && x.Event == "Compounding Interest");
            Assert.Null(aFeb);

            var bFeb = schB.FirstOrDefault(x => x.EndDate == new DateTime(2026, 2, 1) && x.Event == "Compounding Interest");
            Assert.NotNull(bFeb);
            Assert.True(bFeb.ClosingBalance > 1000m);
        }
        [Fact]
        public void Test12_ScratchOutput()
        {
            var fd = CreateFD(85000m, new DateTime(2026, 1, 1), new DateTime(2026, 7, 1));
            
            var interestA = CreateInterest(5m, "30/360", "Monthly", true, "Quarterly", "MATURITY");
            var scheduleA = FDScheduleEngine.GenerateSchedule(fd, interestA);
            
            var interestB = CreateInterest(5m, "30/360", "Monthly", true, "Monthly", "MATURITY");
            var scheduleB = FDScheduleEngine.GenerateSchedule(fd, interestB);

            using var writer = new System.IO.StreamWriter(@"d:\FDManagementSystem\Backend\schedule_output.txt");
            writer.WriteLine("=== SCENARIO A: Monthly Interest, Quarterly Compounding ===");
            PrintSchedule(scheduleA, writer);

            writer.WriteLine();
            writer.WriteLine("=== SCENARIO B: Monthly Interest, Monthly Compounding ===");
            PrintSchedule(scheduleB, writer);
        }

        private void PrintSchedule(List<FinTrustFDManager.Model.Entities.Investment.FDCashFlow> schedule, System.IO.StreamWriter writer)
        {
            writer.WriteLine("Event                | Start  | End    | Days |    Opening | Interest |    Closing | CashFlow | Direction");
            writer.WriteLine("-------------------------------------------------------------------------------------------------------------");
            foreach (var row in schedule)
            {
                writer.WriteLine($"{row.Event,-20} | {row.StartDate:dd-MMM} | {row.EndDate:dd-MMM} | {row.Days,4} | {row.OpeningBalance,10:F2} | {row.InterestAmount,8:F2} | {row.ClosingBalance,10:F2} | {row.CashFlowAmount,8:F2} | {row.Direction}");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // Spec-required tests: AccruedInterest / CapitalizedInterest
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Spec test 1: AccruedInterest increases after each Interest event.
        /// Each Interest row's AccruedInterest must equal the running sum of
        /// all InterestAmount values up to and including that row.
        /// </summary>
        [Fact]
        public void Spec01_AccruedInterest_IncreasesAfterEachInterestEvent()
        {
            var fd = CreateFD(85_000m, new DateTime(2026, 1, 1), new DateTime(2026, 7, 1));
            var interest = CreateInterest(5m, "30/360", "Monthly", true, "Quarterly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            // AccruedInterest increases within each compounding cycle, then resets to 0
            // when a Compounding Interest event capitalizes it. Walk the schedule in
            // its natural emission order (engine guarantees Interest before Compounding
            // for the same date) and verify monotonic increase within each cycle.
            decimal prevAccrued = 0m;
            foreach (var row in schedule)
            {
                if (row.Event == "Compounding Interest")
                {
                    // After capitalization the next cycle starts fresh from 0.
                    prevAccrued = 0m;
                }
                else if (row.Event == "Interest")
                {
                    Assert.True(row.AccruedInterest > prevAccrued,
                        $"Interest row ending {row.EndDate:dd-MMM}: AccruedInterest {row.AccruedInterest:F2} must be > previous value {prevAccrued:F2} in the current cycle");
                    Assert.True(row.AccruedInterest > 0m,
                        $"Interest row ending {row.EndDate:dd-MMM}: AccruedInterest must be positive");
                    prevAccrued = row.AccruedInterest;
                }
            }
        }


        /// <summary>
        /// Spec test 2: CapitalizedInterest is zero on all Interest events.
        /// </summary>
        [Fact]
        public void Spec02_CapitalizedInterest_IsZeroOnInterestEvents()
        {
            var fd = CreateFD(85_000m, new DateTime(2026, 1, 1), new DateTime(2026, 7, 1));
            var interest = CreateInterest(5m, "30/360", "Monthly", true, "Quarterly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var interestRows = schedule.Where(x => x.Event == "Interest").ToList();
            Assert.NotEmpty(interestRows);
            Assert.All(interestRows, row =>
                Assert.True(0m == row.CapitalizedInterest,
                    $"CapitalizedInterest must be 0 on Interest rows, got {row.CapitalizedInterest}"));
        }

        /// <summary>
        /// Spec test 3: InterestAmount is zero on all Compounding Interest events.
        /// The compounding row capitalizes previously-accrued interest; it must not
        /// recalculate new period interest.
        /// </summary>
        [Fact]
        public void Spec03_InterestAmount_IsZeroOnCompoundingInterestEvents()
        {
            var fd = CreateFD(85_000m, new DateTime(2026, 1, 1), new DateTime(2026, 7, 1));
            var interest = CreateInterest(5m, "30/360", "Monthly", true, "Quarterly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var compRows = schedule.Where(x => x.Event == "Compounding Interest").ToList();
            Assert.NotEmpty(compRows);
            Assert.All(compRows, row =>
                Assert.True(0m == row.InterestAmount,
                    $"InterestAmount must be 0 on Compounding rows, got {row.InterestAmount}"));
        }

        /// <summary>
        /// Spec test 4: CapitalizedInterest on a Compounding row equals the
        /// accumulated AccruedInterest from the preceding Interest rows in the cycle.
        /// </summary>
        [Fact]
        public void Spec04_CapitalizedInterest_ContainsAccumulatedAccruedAmount()
        {
            // ₹85,000 | 5% | Monthly Interest | Quarterly Compounding | 30/360 | MATURITY
            var fd = CreateFD(85_000m, new DateTime(2026, 1, 1), new DateTime(2026, 7, 1));
            var interest = CreateInterest(5m, "30/360", "Monthly", true, "Quarterly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            // First compounding boundary: Apr 1. The 3 Interest rows (Feb1/Mar1/Apr1)
            // each add ≈354.17 → AccruedInterest on Apr1 Interest row ≈ 1,062.50.
            var comp1 = schedule.First(x => x.Event == "Compounding Interest");
            Assert.True(comp1.CapitalizedInterest > 0m,
                $"CapitalizedInterest must be positive, got {comp1.CapitalizedInterest}");

            // The CapitalizedInterest should equal the AccruedInterest of the last
            // Interest row ending on the same compounding date.
            var lastInterestBeforeComp = schedule
                .Where(x => x.Event == "Interest" && x.EndDate == comp1.EndDate)
                .OrderBy(x => x.EndDate)
                .LastOrDefault();
            Assert.NotNull(lastInterestBeforeComp);
            Assert.True(lastInterestBeforeComp.AccruedInterest == comp1.CapitalizedInterest,
                $"CapitalizedInterest {comp1.CapitalizedInterest} must match last AccruedInterest {lastInterestBeforeComp.AccruedInterest}");
        }

        /// <summary>
        /// Spec test 5: AccruedInterest resets to zero on a Compounding Interest row.
        /// </summary>
        [Fact]
        public void Spec05_AccruedInterest_ResetsToZeroAfterCapitalization()
        {
            var fd = CreateFD(85_000m, new DateTime(2026, 1, 1), new DateTime(2026, 7, 1));
            var interest = CreateInterest(5m, "30/360", "Monthly", true, "Quarterly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var compRows = schedule.Where(x => x.Event == "Compounding Interest").ToList();
            Assert.NotEmpty(compRows);
            Assert.All(compRows, row =>
                Assert.True(0m == row.AccruedInterest,
                    $"AccruedInterest on Compounding row must be 0 after capitalization, got {row.AccruedInterest}"));
        }

        /// <summary>
        /// Spec test 6: On a Compounding row ClosingBalance = OpeningBalance + CapitalizedInterest.
        /// </summary>
        [Fact]
        public void Spec06_ClosingBalance_IncreasesByCapitalizedInterest()
        {
            var fd = CreateFD(85_000m, new DateTime(2026, 1, 1), new DateTime(2026, 7, 1));
            var interest = CreateInterest(5m, "30/360", "Monthly", true, "Quarterly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var compRows = schedule.Where(x => x.Event == "Compounding Interest").ToList();
            Assert.NotEmpty(compRows);
            Assert.All(compRows, row =>
                Assert.True(row.OpeningBalance + row.CapitalizedInterest == row.ClosingBalance,
                    $"ClosingBalance {row.ClosingBalance} must be OpeningBalance {row.OpeningBalance} + CapitalizedInterest {row.CapitalizedInterest}"));
        }

        /// <summary>
        /// Spec test 7: CASH payment convention — periodic Interest rows must produce
        /// an actual INFLOW with CashFlowAmount > 0.
        /// </summary>
        [Fact]
        public void Spec07_CASH_PaymentConvention_ProducesActualExternalInflow()
        {
            var fd = CreateFD(85_000m, new DateTime(2026, 1, 1), new DateTime(2026, 4, 1));
            var interest = CreateInterest(5m, "30/360", "Monthly", false, "Monthly", "CASH");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var interestRows = schedule.Where(x => x.Event == "Interest").ToList();
            Assert.NotEmpty(interestRows);
            Assert.All(interestRows, row =>
            {
                Assert.True(row.CashFlowAmount > 0m,
                    $"CASH convention: CashFlowAmount must be > 0, got {row.CashFlowAmount}");
                Assert.Equal("INFLOW", row.Direction);
            });
        }

        /// <summary>
        /// Spec test 8: MATURITY payment convention — periodic Interest rows must NOT
        /// produce any external payout (CashFlowAmount = 0, Direction = INTERNAL).
        /// </summary>
        [Fact]
        public void Spec08_MATURITY_PaymentConvention_NoPeriodicExternalPayout()
        {
            var fd = CreateFD(85_000m, new DateTime(2026, 1, 1), new DateTime(2026, 4, 1));
            var interest = CreateInterest(5m, "30/360", "Monthly", false, "Monthly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var interestRows = schedule.Where(x => x.Event == "Interest").ToList();
            Assert.NotEmpty(interestRows);
            Assert.All(interestRows, row =>
            {
                Assert.True(0m == row.CashFlowAmount,
                    $"MATURITY convention: CashFlowAmount must be 0, got {row.CashFlowAmount}");
                Assert.Equal("INTERNAL", row.Direction);
            });
        }

        /// <summary>
        /// Spec test 9: Final partial-period interest must not be lost.
        /// When maturity falls between normal frequency dates, the final economic interest
        /// appears in an Interest row before any Compounding or Maturity row.
        /// </summary>
        [Fact]
        public void Spec09_FinalPartialPeriod_InterestNotLost()
        {
            // Start Apr-1, Monthly interest, Quarterly compounding, maturity Jul-15
            // The final partial period (Jul-1 → Jul-15) must produce an Interest row.
            var fd = CreateFD(85_000m, new DateTime(2026, 4, 1), new DateTime(2026, 7, 15));
            var interest = CreateInterest(5m, "30/360", "Monthly", true, "Quarterly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            // There must be an Interest row whose EndDate is Jul-15 (partial period).
            var partialInterest = schedule
                .Where(x => x.Event == "Interest" && x.EndDate == new DateTime(2026, 7, 15))
                .ToList();
            Assert.NotEmpty(partialInterest);
            Assert.True(partialInterest[0].InterestAmount > 0m,
                $"Final partial-period Interest row must have InterestAmount > 0, got {partialInterest[0].InterestAmount}");
            Assert.True(partialInterest[0].AccruedInterest > 0m,
                $"Final partial-period Interest row must have AccruedInterest > 0, got {partialInterest[0].AccruedInterest}");
        }

        /// <summary>
        /// Spec test 10: TotalInterest must equal Sum(InterestAmount from Interest events only).
        /// Counting CapitalizedInterest would double-count compounded interest.
        /// </summary>
        [Fact]
        public void Spec10_TotalInterest_DoesNotDoubleCountCapitalizedInterest()
        {
            var fd = CreateFD(85_000m, new DateTime(2026, 1, 1), new DateTime(2026, 7, 1));
            var interest = CreateInterest(5m, "30/360", "Monthly", true, "Quarterly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            // Preferred formula: sum only InterestAmount on Interest events.
            decimal correctTotal = schedule
                .Where(x => x.Event == "Interest")
                .Sum(x => x.InterestAmount);

            // Wrong formula: summing InterestAmount + CapitalizedInterest double-counts.
            decimal wrongTotal = schedule.Sum(x => x.InterestAmount + x.CapitalizedInterest);

            Assert.True(correctTotal > 0m, "TotalInterest should be positive");
            Assert.NotEqual(wrongTotal, correctTotal); // Must differ when compounding is present
            Assert.True(correctTotal < wrongTotal,
                $"Correct total {correctTotal} should be < wrong (double-counted) total {wrongTotal}");
        }

        /// <summary>
        /// Spec test 11: Full spec example walkthrough.
        /// Principal=₹85,000 | Rate=5% | Interest=Monthly | Compounding=Quarterly
        /// Basis=30/360 | Convention=MATURITY
        ///
        /// Expected first three Interest rows:
        ///   AccruedInterest = 354.17 → 708.33 → 1,062.50
        ///   CapitalizedInterest = 0 on all
        ///
        /// First Compounding Interest row:
        ///   InterestAmount = 0, CapitalizedInterest = 1,062.50, AccruedInterest = 0
        ///   ClosingBalance = 85,000 + 1,062.50 = 86,062.50
        /// </summary>
        [Fact]
        public void Spec11_MonthlyInterest_QuarterlyCompounding_SpecExample()
        {
            var fd = CreateFD(85_000m, new DateTime(2026, 1, 1), new DateTime(2026, 7, 1));
            var interest = CreateInterest(5m, "30/360", "Monthly", true, "Quarterly", "MATURITY");

            var schedule = FDScheduleEngine.GenerateSchedule(fd, interest);

            var interestRows = schedule
                .Where(x => x.Event == "Interest")
                .OrderBy(x => x.EndDate)
                .ToList();

            // Must have at least 3 Interest rows in first quarter.
            Assert.True(interestRows.Count >= 3, $"Expected at least 3 Interest rows, got {interestRows.Count}");

            // First three rows: InterestAmount ≈ 354.17 each (30 days each in 30/360).
            Assert.Equal(354.17m, interestRows[0].InterestAmount);
            Assert.Equal(354.17m, interestRows[1].InterestAmount);
            Assert.Equal(354.17m, interestRows[2].InterestAmount);

            // AccruedInterest cumulates.
            Assert.Equal(354.17m,  interestRows[0].AccruedInterest);
            Assert.Equal(708.33m,  Math.Round(interestRows[1].AccruedInterest, 2, MidpointRounding.AwayFromZero));
            Assert.Equal(1062.50m, Math.Round(interestRows[2].AccruedInterest, 2, MidpointRounding.AwayFromZero));

            // CapitalizedInterest = 0 on all Interest rows.
            Assert.Equal(0m, interestRows[0].CapitalizedInterest);
            Assert.Equal(0m, interestRows[1].CapitalizedInterest);
            Assert.Equal(0m, interestRows[2].CapitalizedInterest);

            // First Compounding Interest row (Apr-1).
            var comp1 = schedule.First(x => x.Event == "Compounding Interest");
            Assert.Equal(0m,         comp1.InterestAmount);
            Assert.Equal(0m,         comp1.AccruedInterest);
            Assert.Equal(1062.50m,   Math.Round(comp1.CapitalizedInterest, 2, MidpointRounding.AwayFromZero));
            Assert.Equal(86_062.50m, Math.Round(comp1.ClosingBalance, 2, MidpointRounding.AwayFromZero));
            Assert.Equal(0m,         comp1.CashFlowAmount);
            Assert.Equal("INTERNAL", comp1.Direction);
        }

        /// <summary>
        /// Spec test 12: Calculation basis is configuration-driven.
        /// 30/360 must produce exactly 30 days for a calendar month and a deterministic
        /// interest amount. Actual/365 for the same period must differ.
        /// </summary>
        [Fact]
        public void Spec12_CalculationBasis_IsConfigurationDriven()
        {
            var fd30360 = CreateFD(85_000m, new DateTime(2026, 1, 1), new DateTime(2026, 2, 1));
            var int30360 = CreateInterest(5m, "30/360", "At Maturity", false, "At Maturity", "MATURITY");
            var sch30360 = FDScheduleEngine.GenerateSchedule(fd30360, int30360);

            var fdActual = CreateFD(85_000m, new DateTime(2026, 1, 1), new DateTime(2026, 2, 1));
            var intActual = CreateInterest(5m, "Actual/365", "At Maturity", false, "At Maturity", "MATURITY");
            var schActual = FDScheduleEngine.GenerateSchedule(fdActual, intActual);

            var row30360 = sch30360.First(x => x.Event == "Maturity");
            var rowActual = schActual.First(x => x.Event == "Maturity");

            // 30/360: Jan-1 to Feb-1 = exactly 30 days → 85000 * 0.05 * 30/360 = 354.17
            Assert.Equal(30, row30360.Days);
            Assert.Equal(354.17m, row30360.InterestAmount);

            // Actual/365: Jan-1 to Feb-1 = 31 days → 85000 * 0.05 * 31/365 = 360.96
            Assert.Equal(31, rowActual.Days);
            Assert.Equal(360.96m, rowActual.InterestAmount);

            // They must differ because the basis differs.
            Assert.NotEqual(row30360.InterestAmount, rowActual.InterestAmount);
        }
    }
}
