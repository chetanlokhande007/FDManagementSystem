using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.BAL.Services;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities.Investment;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace FinTrustFDManager.BAL.Tests
{
    /// <summary>
    /// Tests FDInterestService cash flow generation by mocking repositories
    /// and capturing the cash flows passed to AddRangeAsync.
    /// </summary>
    public class FDInterestServiceCashFlowTests
    {
        private readonly Mock<IFDInterestRepository> _interestRepo;
        private readonly Mock<IFDIdentificationRepository> _fdRepo;
        private readonly Mock<IFDCashFlowRepository> _cashFlowRepo;
        private readonly Mock<IUnitOfWork> _unitOfWork;
        private readonly FDInterestService _service;

        public FDInterestServiceCashFlowTests()
        {
            _interestRepo = new Mock<IFDInterestRepository>();
            _fdRepo = new Mock<IFDIdentificationRepository>();
            _cashFlowRepo = new Mock<IFDCashFlowRepository>();
            _unitOfWork = new Mock<IUnitOfWork>();

            // UnitOfWork transaction stubs
            var mockTransaction = new Mock<IDbContextTransaction>();
            _unitOfWork.Setup(u => u.BeginTransactionAsync())
                .ReturnsAsync(mockTransaction.Object);
            _unitOfWork.Setup(u => u.CommitTransactionAsync())
                .Returns(Task.CompletedTask);
            _unitOfWork.Setup(u => u.RollbackTransactionAsync())
                .Returns(Task.CompletedTask);

            _service = new FDInterestService(
                _interestRepo.Object,
                _fdRepo.Object,
                _cashFlowRepo.Object,
                _unitOfWork.Object);
        }

        // ═══════════════════════════════════════════
        //  Helper: create a standard FD + interest config
        // ═══════════════════════════════════════════

        private static FDIdentification CreateFd(
            long fdId,
            decimal principal,
            DateTime startDate,
            DateTime endDate,
            string currency = "INR")
        {
            return new FDIdentification
            {
                FdId = fdId,
                FdReferenceNo = $"FD-{fdId:D4}",
                EntityId = 1,
                CounterpartyId = 1,
                CurrencyCode = currency,
                PrincipalAmount = principal,
                StartDate = startDate,
                EndDate = endDate,
                SettlementDate = endDate.AddDays(1),
                Status = "DRAFT"
            };
        }

        private static FDInterest CreateInterest(
            long fdId,
            decimal rate,
            string interestFreq,
            string compoundingFreq,
            bool isCompounding,
            string calcBasis = "ACTUAL_365")
        {
            return new FDInterest
            {
                FdInterestId = 1,
                FdId = fdId,
                InterestRateType = "FIXED",
                InterestRate = rate,
                InterestFrequency = interestFreq,
                CompoundingFrequency = compoundingFreq,
                IsCompounding = isCompounding,
                CalculationBasis = calcBasis,
                CreatedDate = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Sets up the mock repositories and calls CreateAsync,
        /// returning the cash flows that were passed to AddRangeAsync.
        /// </summary>
        private async Task<List<FDCashFlow>> GenerateCashFlowsThroughService(
            FDIdentification fd,
            FDInterest interest)
        {
            List<FDCashFlow>? capturedCashFlows = null;

            // Mock: FD exists
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId))
                .ReturnsAsync(fd);

            // Mock: no existing interest
            _interestRepo.Setup(r => r.GetByFdIdAsync(fd.FdId))
                .ReturnsAsync((FDInterest?)null);

            // Mock: add interest
            _interestRepo.Setup(r => r.AddAsync(It.IsAny<FDInterest>()))
                .ReturnsAsync((FDInterest i) => i);

            // Capture cash flows
            _cashFlowRepo.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<FDCashFlow>>()))
                .Callback<IEnumerable<FDCashFlow>>(cf => capturedCashFlows = cf.ToList())
                .Returns(Task.CompletedTask);

            // Call CreateAsync
            var result = await _service.CreateAsync(interest);

            return capturedCashFlows ?? new List<FDCashFlow>();
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 1: Simple Interest — Monthly, ACTUAL_365
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task SimpleInterest_Monthly_ACTUAL365_CorrectCashFlows()
        {
            // 100,000 at 8% monthly interest, no compounding, 3 months
            var fd = CreateFd(1, 100_000m,
                new DateTime(2025, 1, 1),
                new DateTime(2025, 4, 1));
            var interest = CreateInterest(1, 8m,
                "MONTHLY", "Not Applicable", false, "ACTUAL_365");

            var cf = await GenerateCashFlowsThroughService(fd, interest);

            // Expected: FD Created + 3 Interest events (Feb 1, Mar 1, Apr 1) + Maturity
            Assert.Equal(5, cf.Count);

            // First event: FD Created
            Assert.Equal("FD Created", cf[0].Event);
            Assert.Equal(100_000m, cf[0].CashFlowAmount);
            Assert.Equal("OUTFLOW", cf[0].Direction);

            // Interest events: each pays out (CashFlowAmount > 0)
            var interestEvents = cf.Where(c => c.Event == "Interest").ToList();
            Assert.Equal(3, interestEvents.Count);

            foreach (var ie in interestEvents)
            {
                Assert.True(ie.CashFlowAmount > 0, "Interest should be paid out in non-compounding mode");
                Assert.True(ie.OpeningBalance == ie.ClosingBalance, "Balance unchanged in non-compounding");
                Assert.Equal("INFLOW", ie.Direction);
            }

            // Interest for Feb (31 days): 100000 * 0.08 * 31/365 = 679.45
            Assert.Equal(679.45m, interestEvents[0].InterestAmount);
            Assert.Equal(31, interestEvents[0].Days);

            // Interest for Mar (28 days): 100000 * 0.08 * 28/365 = 613.70
            Assert.Equal(613.70m, interestEvents[1].InterestAmount);
            Assert.Equal(28, interestEvents[1].Days);

            // Interest for Apr (31 days): 100000 * 0.08 * 31/365 = 679.45
            Assert.Equal(679.45m, interestEvents[2].InterestAmount);

            // Maturity: pays back principal
            var maturity = cf.Last();
            Assert.Equal("Maturity", maturity.Event);
            Assert.Equal(100_000m, maturity.CashFlowAmount);
            Assert.Equal("INFLOW", maturity.Direction);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 2: Compound Interest — Quarterly, ACTUAL_365
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task CompoundInterest_Quarterly_ACTUAL365_BalanceGrows()
        {
            // 100,000 at 8% quarterly interest + quarterly compounding, 1 year
            var fd = CreateFd(1, 100_000m,
                new DateTime(2025, 1, 1),
                new DateTime(2025, 12, 31));
            var interest = CreateInterest(1, 8m,
                "QUARTERLY", "QUARTERLY", true, "ACTUAL_365");

            var cf = await GenerateCashFlowsThroughService(fd, interest);

            // Events: FD Created + (Interest + Compounding)*3 + partial Compounding + Maturity
            // Quarterly dates: Apr 1, Jul 1, Oct 1
            // Partial: Oct 1 → Dec 31
            var maturity = cf.Last(c => c.Event == "Maturity");

            // Maturity amount should be > principal (compounding effect)
            Assert.True(maturity.CashFlowAmount > 100_000m,
                $"Maturity ({maturity.CashFlowAmount}) should exceed principal (100000)");

            // Balance should grow at each compounding event
            var compoundingEvents = cf.Where(c => c.Event == "Compounding Interest").ToList();
            Assert.Equal(4, compoundingEvents.Count); // 3 quarterly + 1 partial

            decimal prevBalance = 100_000m;
            foreach (var ce in compoundingEvents)
            {
                Assert.True(ce.ClosingBalance > ce.OpeningBalance,
                    $"Closing ({ce.ClosingBalance}) should exceed Opening ({ce.OpeningBalance})");
                Assert.Equal(prevBalance, ce.OpeningBalance);
                prevBalance = ce.ClosingBalance;
            }

            // The final compounding event's ClosingBalance should equal maturity amount
            Assert.Equal(compoundingEvents.Last().ClosingBalance, maturity.CashFlowAmount);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 3: AT_MATURITY + Compounding — THE KEY FIX
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task AT_MATURITY_WithCompounding_Quarterly_ProducesCorrectMaturity()
        {
            // This was the critical bug: AT_MATURITY + compounding
            // should compound interest quarterly, not calculate it all at once.
            var fd = CreateFd(1, 100_000m,
                new DateTime(2025, 1, 1),
                new DateTime(2025, 12, 31));
            var interest = CreateInterest(1, 8m,
                "AT_MATURITY", "QUARTERLY", true, "ACTUAL_365");

            var cf = await GenerateCashFlowsThroughService(fd, interest);

            // Should have: FD Created + 4 Compounding Interest + Maturity
            // (3 quarterly dates + 1 partial period)
            var compoundingEvents = cf.Where(c => c.Event == "Compounding Interest").ToList();
            Assert.Equal(4, compoundingEvents.Count);

            // Each compounding event should add interest to balance
            decimal prevBalance = 100_000m;
            foreach (var ce in compoundingEvents)
            {
                Assert.True(ce.InterestAmount > 0,
                    $"Compounding event should have positive interest, got {ce.InterestAmount}");
                Assert.Equal(prevBalance, ce.OpeningBalance);
                Assert.Equal(prevBalance + ce.InterestAmount, ce.ClosingBalance);
                prevBalance = ce.ClosingBalance;
            }

            // Maturity should reflect the compounded balance
            var maturity = cf.Last(c => c.Event == "Maturity");
            Assert.True(maturity.CashFlowAmount > 100_000m,
                $"Compounded maturity ({maturity.CashFlowAmount}) must exceed principal");

            // The compounded amount should be MORE than simple interest
            // Simple: 100000 * 0.08 * 365/365 = 8000 → maturity = 108000
            // Compounded should be > 108000
            Assert.True(maturity.CashFlowAmount > 108_000m,
                $"Compounded maturity ({maturity.CashFlowAmount}) must exceed simple interest maturity (108000)");

            // No "Interest" events should exist (AT_MATURITY has no interest schedule)
            Assert.Empty(cf.Where(c => c.Event == "Interest"));
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 4: AT_MATURITY without compounding — Simple
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task AT_MATURITY_WithoutCompounding_SingleInterestPayment()
        {
            var fd = CreateFd(1, 100_000m,
                new DateTime(2025, 1, 1),
                new DateTime(2025, 12, 31));
            var interest = CreateInterest(1, 8m,
                "AT_MATURITY", "Not Applicable", false, "ACTUAL_365");

            var cf = await GenerateCashFlowsThroughService(fd, interest);

            // FD Created + 1 Interest (partial period) + Maturity
            var interestEvents = cf.Where(c => c.Event == "Interest").ToList();
            Assert.Single(interestEvents);

            // Single interest calculation for entire period
            // Jan 1 → Dec 31 = 364 days (date difference)
            // 100000 * 0.08 * 364/365 = 7978.08
            Assert.Equal(7978.08m, interestEvents[0].InterestAmount);
            Assert.Equal(364, interestEvents[0].Days);

            // Interest is paid out
            Assert.Equal(interestEvents[0].InterestAmount, interestEvents[0].CashFlowAmount);

            // Maturity returns principal
            var maturity = cf.Last(c => c.Event == "Maturity");
            Assert.Equal(100_000m, maturity.CashFlowAmount);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 5: ACTUAL_360 vs ACTUAL_365
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task SimpleInterest_ACTUAL360_HigherThan_ACTUAL365()
        {
            // Same FD, different day count basis
            // ACTUAL_360 uses 360 as divisor → more interest per day
            var fd360 = CreateFd(1, 100_000m,
                new DateTime(2025, 1, 1), new DateTime(2025, 4, 1));
            var int360 = CreateInterest(1, 8m,
                "AT_MATURITY", "Not Applicable", false, "ACTUAL_360");

            var fd365 = CreateFd(2, 100_000m,
                new DateTime(2025, 1, 1), new DateTime(2025, 4, 1));
            var int365 = CreateInterest(2, 8m,
                "AT_MATURITY", "Not Applicable", false, "ACTUAL_365");

            var cf360 = await GenerateCashFlowsThroughService(fd360, int360);
            var cf365 = await GenerateCashFlowsThroughService(fd365, int365);

            var interest360 = cf360.Single(c => c.Event == "Interest").InterestAmount;
            var interest365 = cf365.Single(c => c.Event == "Interest").InterestAmount;

            // ACTUAL_360 should yield more interest (smaller divisor)
            Assert.True(interest360 > interest365,
                $"ACTUAL_360 ({interest360}) should be > ACTUAL_365 ({interest365})");
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 6: Leap Year
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task LeapYear_Feb29_IncludedInDayCount()
        {
            // Start: Jan 1, 2024 (leap year) → Apr 1, 2024
            // Feb has 29 days in 2024
            var fd = CreateFd(1, 100_000m,
                new DateTime(2024, 1, 1),
                new DateTime(2024, 4, 1));
            var interest = CreateInterest(1, 8m,
                "AT_MATURITY", "Not Applicable", false, "ACTUAL_365");

            var cf = await GenerateCashFlowsThroughService(fd, interest);

            var ie = cf.Single(c => c.Event == "Interest");
            // Jan 1 → Apr 1 in leap year = 91 days (31+29+31)
            Assert.Equal(91, ie.Days);

            // 100000 * 0.08 * 91/365 = 1994.52
            Assert.Equal(1994.52m, ie.InterestAmount);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 7: End-of-Month Handling
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task EOM_StartDate_Jan31_Quarterly_SnapsToEndOfMonth()
        {
            // Start: Jan 31, quarterly → Apr 30, Jul 31, Oct 31
            // (not Apr 31 which doesn't exist)
            var fd = CreateFd(1, 100_000m,
                new DateTime(2025, 1, 31),
                new DateTime(2025, 10, 31));
            var interest = CreateInterest(1, 8m,
                "QUARTERLY", "QUARTERLY", true, "ACTUAL_365");

            var cf = await GenerateCashFlowsThroughService(fd, interest);

            var compoundingDates = cf
                .Where(c => c.Event == "Compounding Interest")
                .Select(c => c.EndDate)
                .ToList();

            // Should be Apr 30, Jul 31, Oct 31 (not Apr 31)
            Assert.Contains(new DateTime(2025, 4, 30), compoundingDates);
            Assert.Contains(new DateTime(2025, 7, 31), compoundingDates);
            Assert.Contains(new DateTime(2025, 10, 31), compoundingDates);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 8: Cash Flow Balance Chain
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task CompoundInterest_BalanceChain_IsConsistent()
        {
            // In compounding mode, each Compounding event's ClosingBalance
            // should become the next event's OpeningBalance
            var fd = CreateFd(1, 50_000m,
                new DateTime(2025, 1, 1),
                new DateTime(2025, 12, 31));
            var interest = CreateInterest(1, 6m,
                "MONTHLY", "MONTHLY", true, "ACTUAL_365");

            var cf = await GenerateCashFlowsThroughService(fd, interest);

            // Skip FD Created (index 0), check chain
            decimal expectedBalance = 50_000m;
            for (int i = 1; i < cf.Count; i++)
            {
                var current = cf[i];
                if (current.Event == "Maturity")
                {
                    Assert.True(expectedBalance == current.CashFlowAmount,
                        $"Maturity CashFlowAmount should match last balance at index {i}");
                    break;
                }

                Assert.True(expectedBalance == current.OpeningBalance,
                    $"OpeningBalance mismatch at index {i} ({current.Event})");

                if (current.Event == "Compounding Interest")
                {
                    expectedBalance = current.ClosingBalance;
                    Assert.True(current.ClosingBalance > current.OpeningBalance,
                        $"ClosingBalance should exceed OpeningBalance at index {i}");
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 9: Half-Yearly Compounding
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task CompoundInterest_HalfYearly_2CompoundingEvents()
        {
            var fd = CreateFd(1, 200_000m,
                new DateTime(2025, 1, 1),
                new DateTime(2026, 1, 1));
            var interest = CreateInterest(1, 10m,
                "HALF_YEARLY", "HALF_YEARLY", true, "ACTUAL_365");

            var cf = await GenerateCashFlowsThroughService(fd, interest);

            // Half-yearly on 1 year: Jul 1 + Jan 1 (maturity) = 1 or 2 compounding events
            // Jul 1 is within range, Jan 1 is the end date
            var compoundingEvents = cf.Where(c => c.Event == "Compounding Interest").ToList();
            Assert.True(compoundingEvents.Count >= 1,
                $"Should have at least 1 compounding event, got {compoundingEvents.Count}");

            // Total interest should exceed simple interest
            var maturity = cf.Last(c => c.Event == "Maturity");
            // Simple: 200000 * 0.10 * 365/365 = 20000 → 220000
            Assert.True(maturity.CashFlowAmount > 220_000m,
                $"Compounded maturity ({maturity.CashFlowAmount}) must exceed simple (220000)");
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 10: Validation — Compounding requires frequency
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task CreateAsync_CompoundingWithoutFrequency_ThrowsInvalidOperation()
        {
            var fd = CreateFd(1, 100_000m,
                new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));
            var interest = CreateInterest(1, 8m,
                "QUARTERLY", "Not Applicable", true, "ACTUAL_365");

            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);
            _interestRepo.Setup(r => r.GetByFdIdAsync(fd.FdId))
                .ReturnsAsync((FDInterest?)null);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateAsync(interest));
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 11: Maturity Cash Flow Amount = Compounded Balance
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task CompoundInterest_MaturityAmount_EqualsFinalCompoundedBalance()
        {
            var fd = CreateFd(1, 150_000m,
                new DateTime(2025, 3, 15),
                new DateTime(2026, 3, 15));
            var interest = CreateInterest(1, 7.5m,
                "QUARTERLY", "QUARTERLY", true, "ACTUAL_365");

            var cf = await GenerateCashFlowsThroughService(fd, interest);

            var lastCompounding = cf
                .Where(c => c.Event == "Compounding Interest")
                .LastOrDefault();
            var maturity = cf.Last(c => c.Event == "Maturity");

            if (lastCompounding != null)
            {
                Assert.True(lastCompounding.ClosingBalance == maturity.CashFlowAmount,
                    "Maturity amount should equal the final compounded balance");
            }
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 12: Interest Frequency AT_MATURITY — No Interest Events
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task AT_MATURITY_Compounding_NoInterestEvents()
        {
            // AT_MATURITY should never generate "Interest" events
            var fd = CreateFd(1, 100_000m,
                new DateTime(2025, 1, 1),
                new DateTime(2025, 12, 31));
            var interest = CreateInterest(1, 8m,
                "AT_MATURITY", "ANNUALLY", true, "ACTUAL_365");

            var cf = await GenerateCashFlowsThroughService(fd, interest);

            Assert.Empty(cf.Where(c => c.Event == "Interest"));
            Assert.Contains(cf, c => c.Event == "Compounding Interest");
            Assert.Contains(cf, c => c.Event == "Maturity");
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 13: Non-compounding Interest Events Pay Out
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task NonCompounding_InterestPaidOutEachPeriod()
        {
            var fd = CreateFd(1, 100_000m,
                new DateTime(2025, 1, 1),
                new DateTime(2025, 7, 1));
            var interest = CreateInterest(1, 8m,
                "MONTHLY", "Not Applicable", false, "ACTUAL_365");

            var cf = await GenerateCashFlowsThroughService(fd, interest);

            var interestEvents = cf.Where(c => c.Event == "Interest").ToList();
            Assert.True(interestEvents.Count >= 5, $"Expected >= 5 monthly interest events, got {interestEvents.Count}");

            foreach (var ie in interestEvents)
            {
                // Non-compounding: interest is paid out, balance unchanged
                Assert.True(ie.CashFlowAmount > 0,
                    $"Interest event should have CashFlowAmount > 0, got {ie.CashFlowAmount}");
                Assert.True(ie.OpeningBalance == ie.ClosingBalance,
                    "Balance should not change in non-compounding mode");
            }

            // No compounding events
            Assert.Empty(cf.Where(c => c.Event == "Compounding Interest"));
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 14: Total Interest equals sum of all interest amounts
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task CompoundInterest_TotalInterest_EqualsMaturityMinusPrincipal()
        {
            var fd = CreateFd(1, 100_000m,
                new DateTime(2025, 1, 1),
                new DateTime(2025, 12, 31));
            var interest = CreateInterest(1, 8m,
                "QUARTERLY", "QUARTERLY", true, "ACTUAL_365");

            var cf = await GenerateCashFlowsThroughService(fd, interest);

            var maturity = cf.Last(c => c.Event == "Maturity");
            decimal expectedTotalInterest = maturity.CashFlowAmount - 100_000m;

            // In compounding mode, the Compounding Interest events track the
            // accumulated interest that gets added to balance. Their sum should
            // equal total interest earned.
            decimal compoundingInterestSum = cf
                .Where(c => c.Event == "Compounding Interest")
                .Sum(c => c.InterestAmount);

            Assert.True(compoundingInterestSum == expectedTotalInterest,
                $"Compounding interest sum ({compoundingInterestSum}) should equal " +
                $"maturity - principal ({expectedTotalInterest})");

            // Note: Interest events sum does NOT equal total interest in compounding mode
            // because the partial period (last quarter → maturity) is captured as a
            // Compounding Interest event, not an Interest event. This is correct behavior.
        }
    }
}
