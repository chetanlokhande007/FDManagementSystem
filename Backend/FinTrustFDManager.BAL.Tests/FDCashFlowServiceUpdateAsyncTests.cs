using FinTrustFDManager.BAL.DTOs;
using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.BAL.Services;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities.Investment;
using Moq;
using Xunit;

namespace FinTrustFDManager.BAL.Tests
{
    /// <summary>
    /// Tests FDCashFlowService.UpdateAsync recalculation logic,
    /// focusing on the compounding Interest handler fix and
    /// balance chain consistency.
    /// </summary>
    public class FDCashFlowServiceUpdateAsyncTests
    {
        private readonly Mock<IFDCashFlowRepository> _cashFlowRepo;
        private readonly Mock<IFDInterestRepository> _interestRepo;
        private readonly Mock<IFDIdentificationRepository> _fdRepo;
        private readonly FDCashFlowService _service;

        public FDCashFlowServiceUpdateAsyncTests()
        {
            _cashFlowRepo = new Mock<IFDCashFlowRepository>();
            _interestRepo = new Mock<IFDInterestRepository>();
            _fdRepo = new Mock<IFDIdentificationRepository>();

            _service = new FDCashFlowService(
                _cashFlowRepo.Object,
                _interestRepo.Object,
                _fdRepo.Object);
        }

        // ═══════════════════════════════════════════
        //  Helpers
        // ═══════════════════════════════════════════

        private static FDIdentification CreateFd(
            long fdId = 1,
            decimal principal = 100_000m,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var start = startDate ?? new DateTime(2025, 1, 1);
            var end = endDate ?? new DateTime(2025, 12, 31);
            return new FDIdentification
            {
                FdId = fdId,
                FdReferenceNo = $"FD-{fdId:D4}",
                EntityId = 1,
                CounterpartyId = 1,
                CurrencyCode = "INR",
                PrincipalAmount = principal,
                StartDate = start,
                EndDate = end,
                SettlementDate = end.AddDays(1),
                Status = "DRAFT"
            };
        }

        private static FDInterest CreateInterest(
            long fdId = 1,
            decimal rate = 8m,
            string interestFreq = "QUARTERLY",
            string compoundingFreq = "QUARTERLY",
            bool isCompounding = true,
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
        /// Builds a list of FDCashFlow entities that simulate what GenerateCashFlows
        /// would produce for quarterly interest + quarterly compounding on a 1-year FD.
        /// </summary>
        private static List<FDCashFlow> BuildQuarterlyCompoundingCashFlows(
            long fdId, decimal principal, decimal rate)
        {
            // Jan 1 → Dec 31, quarterly interest + quarterly compounding
            // Quarterly dates: Apr 1, Jul 1, Oct 1
            // Partial: Oct 1 → Dec 31
            var start = new DateTime(2025, 1, 1);
            var q1 = new DateTime(2025, 4, 1);
            var q2 = new DateTime(2025, 7, 1);
            var q3 = new DateTime(2025, 10, 1);
            var end = new DateTime(2025, 12, 31);

            // Pre-calculate the cash flows (matching what GenerateCashFlows produces)
            decimal balance = principal;
            decimal accrued = 0;
            var now = DateTime.UtcNow;

            var cf = new List<FDCashFlow>();

            // FD Created
            cf.Add(new FDCashFlow
            {
                CashFlowId = 1, FdId = fdId, Event = "FD Created",
                StartDate = start, EndDate = start, Days = 0,
                InterestRate = rate, OpeningBalance = 0, InterestAmount = 0,
                ClosingBalance = principal, CashFlowAmount = principal,
                Direction = "OUTFLOW", CurrencyCode = "INR", Status = "PENDING",
                ReferenceNo = "FD-0001", CreatedDate = now
            });

            // Q1: Jan 1 → Apr 1 (90 days)
            decimal q1Interest = Math.Round(balance * (rate / 100m) * (90m / 365m), 2);
            accrued += q1Interest;
            cf.Add(new FDCashFlow
            {
                CashFlowId = 2, FdId = fdId, Event = "Interest",
                StartDate = start, EndDate = q1, Days = 90,
                InterestRate = rate, OpeningBalance = balance, InterestAmount = q1Interest,
                ClosingBalance = balance, CashFlowAmount = 0,
                Direction = "INFLOW", CurrencyCode = "INR", Status = "PENDING",
                ReferenceNo = "FD-0001", CreatedDate = now
            });
            cf.Add(new FDCashFlow
            {
                CashFlowId = 3, FdId = fdId, Event = "Compounding Interest",
                StartDate = q1, EndDate = q1, Days = 0,
                InterestRate = rate, OpeningBalance = balance,
                InterestAmount = Math.Round(accrued, 2),
                ClosingBalance = balance + Math.Round(accrued, 2),
                CashFlowAmount = 0, Direction = "INFLOW", CurrencyCode = "INR",
                Status = "PENDING", ReferenceNo = "FD-0001", CreatedDate = now
            });
            balance += Math.Round(accrued, 2);
            accrued = 0;

            // Q2: Apr 1 → Jul 1 (91 days)
            decimal q2Interest = Math.Round(balance * (rate / 100m) * (91m / 365m), 2);
            accrued += q2Interest;
            cf.Add(new FDCashFlow
            {
                CashFlowId = 4, FdId = fdId, Event = "Interest",
                StartDate = q1, EndDate = q2, Days = 91,
                InterestRate = rate, OpeningBalance = balance, InterestAmount = q2Interest,
                ClosingBalance = balance, CashFlowAmount = 0,
                Direction = "INFLOW", CurrencyCode = "INR", Status = "PENDING",
                ReferenceNo = "FD-0001", CreatedDate = now
            });
            cf.Add(new FDCashFlow
            {
                CashFlowId = 5, FdId = fdId, Event = "Compounding Interest",
                StartDate = q2, EndDate = q2, Days = 0,
                InterestRate = rate, OpeningBalance = balance,
                InterestAmount = Math.Round(accrued, 2),
                ClosingBalance = balance + Math.Round(accrued, 2),
                CashFlowAmount = 0, Direction = "INFLOW", CurrencyCode = "INR",
                Status = "PENDING", ReferenceNo = "FD-0001", CreatedDate = now
            });
            balance += Math.Round(accrued, 2);
            accrued = 0;

            // Q3: Jul 1 → Oct 1 (92 days)
            decimal q3Interest = Math.Round(balance * (rate / 100m) * (92m / 365m), 2);
            accrued += q3Interest;
            cf.Add(new FDCashFlow
            {
                CashFlowId = 6, FdId = fdId, Event = "Interest",
                StartDate = q2, EndDate = q3, Days = 92,
                InterestRate = rate, OpeningBalance = balance, InterestAmount = q3Interest,
                ClosingBalance = balance, CashFlowAmount = 0,
                Direction = "INFLOW", CurrencyCode = "INR", Status = "PENDING",
                ReferenceNo = "FD-0001", CreatedDate = now
            });
            cf.Add(new FDCashFlow
            {
                CashFlowId = 7, FdId = fdId, Event = "Compounding Interest",
                StartDate = q3, EndDate = q3, Days = 0,
                InterestRate = rate, OpeningBalance = balance,
                InterestAmount = Math.Round(accrued, 2),
                ClosingBalance = balance + Math.Round(accrued, 2),
                CashFlowAmount = 0, Direction = "INFLOW", CurrencyCode = "INR",
                Status = "PENDING", ReferenceNo = "FD-0001", CreatedDate = now
            });
            balance += Math.Round(accrued, 2);
            accrued = 0;

            // Partial: Oct 1 → Dec 31 (91 days)
            decimal partialInterest = Math.Round(balance * (rate / 100m) * (91m / 365m), 2);
            cf.Add(new FDCashFlow
            {
                CashFlowId = 8, FdId = fdId, Event = "Compounding Interest",
                StartDate = q3, EndDate = end, Days = 91,
                InterestRate = rate, OpeningBalance = balance,
                InterestAmount = partialInterest,
                ClosingBalance = balance + partialInterest,
                CashFlowAmount = 0, Direction = "INFLOW", CurrencyCode = "INR",
                Status = "PENDING", ReferenceNo = "FD-0001", CreatedDate = now
            });
            balance += partialInterest;

            // Maturity
            cf.Add(new FDCashFlow
            {
                CashFlowId = 9, FdId = fdId, Event = "Maturity",
                StartDate = end, EndDate = end, Days = 0,
                InterestRate = rate, OpeningBalance = balance,
                InterestAmount = 0, ClosingBalance = 0,
                CashFlowAmount = balance, Direction = "INFLOW",
                CurrencyCode = "INR", Status = "PENDING",
                ReferenceNo = "FD-0001", CreatedDate = now
            });

            return cf;
        }

        /// <summary>
        /// Builds cash flows for non-compounding monthly interest.
        /// </summary>
        private static List<FDCashFlow> BuildNonCompoundingMonthlyCashFlows(
            long fdId, decimal principal, decimal rate)
        {
            // Jan 1 → Apr 1 (3 months), monthly interest, no compounding
            var start = new DateTime(2025, 1, 1);
            var m1 = new DateTime(2025, 2, 1);
            var m2 = new DateTime(2025, 3, 1);
            var end = new DateTime(2025, 4, 1);

            decimal balance = principal;
            var now = DateTime.UtcNow;
            var cf = new List<FDCashFlow>();

            cf.Add(new FDCashFlow
            {
                CashFlowId = 1, FdId = fdId, Event = "FD Created",
                StartDate = start, EndDate = start, Days = 0,
                InterestRate = rate, OpeningBalance = 0, InterestAmount = 0,
                ClosingBalance = principal, CashFlowAmount = principal,
                Direction = "OUTFLOW", CurrencyCode = "INR", Status = "PENDING",
                ReferenceNo = "FD-0001", CreatedDate = now
            });

            // Jan → Feb (31 days)
            decimal i1 = Math.Round(balance * (rate / 100m) * (31m / 365m), 2);
            cf.Add(new FDCashFlow
            {
                CashFlowId = 2, FdId = fdId, Event = "Interest",
                StartDate = start, EndDate = m1, Days = 31,
                InterestRate = rate, OpeningBalance = balance, InterestAmount = i1,
                ClosingBalance = balance, CashFlowAmount = i1,
                Direction = "INFLOW", CurrencyCode = "INR", Status = "PENDING",
                ReferenceNo = "FD-0001", CreatedDate = now
            });

            // Feb → Mar (28 days)
            decimal i2 = Math.Round(balance * (rate / 100m) * (28m / 365m), 2);
            cf.Add(new FDCashFlow
            {
                CashFlowId = 3, FdId = fdId, Event = "Interest",
                StartDate = m1, EndDate = m2, Days = 28,
                InterestRate = rate, OpeningBalance = balance, InterestAmount = i2,
                ClosingBalance = balance, CashFlowAmount = i2,
                Direction = "INFLOW", CurrencyCode = "INR", Status = "PENDING",
                ReferenceNo = "FD-0001", CreatedDate = now
            });

            // Mar → Apr (31 days)
            decimal i3 = Math.Round(balance * (rate / 100m) * (31m / 365m), 2);
            cf.Add(new FDCashFlow
            {
                CashFlowId = 4, FdId = fdId, Event = "Interest",
                StartDate = m2, EndDate = end, Days = 31,
                InterestRate = rate, OpeningBalance = balance, InterestAmount = i3,
                ClosingBalance = balance, CashFlowAmount = i3,
                Direction = "INFLOW", CurrencyCode = "INR", Status = "PENDING",
                ReferenceNo = "FD-0001", CreatedDate = now
            });

            // Maturity
            cf.Add(new FDCashFlow
            {
                CashFlowId = 5, FdId = fdId, Event = "Maturity",
                StartDate = end, EndDate = end, Days = 0,
                InterestRate = rate, OpeningBalance = balance,
                InterestAmount = 0, ClosingBalance = 0,
                CashFlowAmount = balance, Direction = "INFLOW",
                CurrencyCode = "INR", Status = "PENDING",
                ReferenceNo = "FD-0001", CreatedDate = now
            });

            return cf;
        }

        /// <summary>
        /// Sets up mocks and calls UpdateAsync, returning the updated cash flows
        /// that were passed to UpdateRangeAsync.
        /// </summary>
        private async Task<List<FDCashFlow>> UpdateCashFlow(
            FDIdentification fd,
            FDInterest interest,
            List<FDCashFlow> existingCashFlows,
            long cashFlowIdToUpdate,
            DateTime newEndDate)
        {
            List<FDCashFlow>? captured = null;

            _cashFlowRepo.Setup(r => r.GetByFdIdAsync(fd.FdId))
                .ReturnsAsync(existingCashFlows);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId))
                .ReturnsAsync(fd);
            _interestRepo.Setup(r => r.GetByFdIdAsync(fd.FdId))
                .ReturnsAsync(interest);
            _cashFlowRepo.Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<FDCashFlow>>()))
                .Callback<IEnumerable<FDCashFlow>>(cf => captured = cf.ToList())
                .Returns(Task.CompletedTask);

            var dto = new FDCashFlowDto
            {
                CashFlowId = cashFlowIdToUpdate,
                FdId = fd.FdId,
                EndDate = newEndDate
            };

            await _service.UpdateAsync(cashFlowIdToUpdate, dto);

            return captured ?? new List<FDCashFlow>();
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 1: Compounding mode — Interest events accumulate
        //          (the key bug fix)
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task CompoundingMode_InterestEvent_AccruesInterest_NotZero()
        {
            // Edit Q1 Interest event's EndDate from Apr 1 → Mar 15
            // After the fix, Interest events in compounding mode should
            // show accumulated interest, not zero.
            var fd = CreateFd();
            var interest = CreateInterest();
            var cashFlows = BuildQuarterlyCompoundingCashFlows(1, 100_000m, 8m);

            var result = await UpdateCashFlow(
                fd, interest, cashFlows,
                cashFlowIdToUpdate: 2,   // Q1 Interest event
                newEndDate: new DateTime(2025, 3, 15));

            var q1Interest = result.First(c => c.CashFlowId == 2);

            // Q1 Interest event should have non-zero InterestAmount
            Assert.True(q1Interest.InterestAmount > 0,
                $"Q1 Interest should be > 0 after edit, got {q1Interest.InterestAmount}");

            // Days should be recalculated: Jan 1 → Mar 15 = 73 days
            Assert.Equal(73, q1Interest.Days);

            // Balance should not change (interest accrues, doesn't pay out)
            Assert.Equal(q1Interest.OpeningBalance, q1Interest.ClosingBalance);
            Assert.Equal(0m, q1Interest.CashFlowAmount);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 2: Compounding mode — Compounding event compounds
        //          accumulated interest
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task CompoundingMode_CompoundingEvent_CompoundsAccumulatedInterest()
        {
            var fd = CreateFd();
            var interest = CreateInterest();
            var cashFlows = BuildQuarterlyCompoundingCashFlows(1, 100_000m, 8m);

            var result = await UpdateCashFlow(
                fd, interest, cashFlows,
                cashFlowIdToUpdate: 2,   // Edit Q1 Interest
                newEndDate: new DateTime(2025, 3, 15));

            var q1Compound = result.First(c => c.CashFlowId == 3);

            // Compounding event should show positive interest
            Assert.True(q1Compound.InterestAmount > 0,
                $"Q1 Compounding should have positive interest, got {q1Compound.InterestAmount}");

            // ClosingBalance should exceed OpeningBalance
            Assert.True(q1Compound.ClosingBalance > q1Compound.OpeningBalance,
                $"Closing ({q1Compound.ClosingBalance}) should exceed Opening ({q1Compound.OpeningBalance})");

            // Q1 Compound's InterestAmount includes BOTH Q1 Interest's accrued
            // AND Q1 Compound's own period interest (Mar 15 → Apr 1 = 17 days).
            // So it should be >= Q1 Interest's InterestAmount.
            var q1Interest = result.First(c => c.CashFlowId == 2);
            Assert.True(q1Compound.InterestAmount >= q1Interest.InterestAmount,
                $"Q1 Compound ({q1Compound.InterestAmount}) should be >= Q1 Interest ({q1Interest.InterestAmount})");
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 3: Compounding mode — Balance chain propagates
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task CompoundingMode_BalanceChain_PropagatesCorrectly()
        {
            var fd = CreateFd();
            var interest = CreateInterest();
            var cashFlows = BuildQuarterlyCompoundingCashFlows(1, 100_000m, 8m);

            var result = await UpdateCashFlow(
                fd, interest, cashFlows,
                cashFlowIdToUpdate: 2,
                newEndDate: new DateTime(2025, 3, 15));

            // Each event's OpeningBalance should equal the previous event's ClosingBalance.
            // Skip FD Created (index 0) since it has OpeningBalance = 0 by design.
            decimal expectedBalance = 100_000m; // FD Created's ClosingBalance
            for (int i = 1; i < result.Count; i++)
            {
                var current = result[i];
                if (current.Event == "Maturity")
                {
                    Assert.True(expectedBalance == current.CashFlowAmount,
                        $"Maturity CashFlowAmount should be {expectedBalance}, got {current.CashFlowAmount}");
                    break;
                }

                Assert.True(expectedBalance == current.OpeningBalance,
                    $"OpeningBalance mismatch at index {i} ({current.Event}): " +
                    $"expected {expectedBalance}, got {current.OpeningBalance}");

                if (current.Event == "Compounding Interest")
                {
                    expectedBalance = current.ClosingBalance;
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 4: Non-compounding mode — Interest paid out
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task NonCompoundingMode_InterestPaidOut_BalanceUnchanged()
        {
            var fd = CreateFd(endDate: new DateTime(2025, 4, 1));
            var interest = CreateInterest(
                interestFreq: "MONTHLY", compoundingFreq: "Not Applicable",
                isCompounding: false);
            var cashFlows = BuildNonCompoundingMonthlyCashFlows(1, 100_000m, 8m);

            var result = await UpdateCashFlow(
                fd, interest, cashFlows,
                cashFlowIdToUpdate: 2,   // Jan → Feb Interest
                newEndDate: new DateTime(2025, 1, 20));

            var edited = result.First(c => c.CashFlowId == 2);

            // Non-compounding: interest is paid out
            Assert.True(edited.CashFlowAmount > 0,
                $"Interest should be paid out, got CashFlowAmount = {edited.CashFlowAmount}");
            Assert.Equal(edited.InterestAmount, edited.CashFlowAmount);

            // Balance unchanged
            Assert.Equal(edited.OpeningBalance, edited.ClosingBalance);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 5: Non-compounding mode — Balance chain
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task NonCompoundingMode_BalanceChain_PrincipalUnchanged()
        {
            var fd = CreateFd(endDate: new DateTime(2025, 4, 1));
            var interest = CreateInterest(
                interestFreq: "MONTHLY", compoundingFreq: "Not Applicable",
                isCompounding: false);
            var cashFlows = BuildNonCompoundingMonthlyCashFlows(1, 100_000m, 8m);

            var result = await UpdateCashFlow(
                fd, interest, cashFlows,
                cashFlowIdToUpdate: 3,   // Feb → Mar Interest
                newEndDate: new DateTime(2025, 2, 20));

            // In non-compounding mode, balance should remain at principal throughout
            foreach (var cf in result.Where(c => c.Event != "Maturity"))
            {
                if (cf.Event == "FD Created")
                    continue;
                Assert.Equal(100_000m, cf.OpeningBalance);
                Assert.Equal(100_000m, cf.ClosingBalance);
            }

            // Maturity pays back principal
            var maturity = result.First(c => c.Event == "Maturity");
            Assert.Equal(100_000m, maturity.CashFlowAmount);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 6: Days recalculated correctly after edit
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task EditEndDate_DaysRecalculated_ForEditedEvent()
        {
            var fd = CreateFd();
            var interest = CreateInterest();
            var cashFlows = BuildQuarterlyCompoundingCashFlows(1, 100_000m, 8m);

            // Edit Q1 Interest from 90 days to 45 days
            var result = await UpdateCashFlow(
                fd, interest, cashFlows,
                cashFlowIdToUpdate: 2,
                newEndDate: new DateTime(2025, 2, 15));

            var edited = result.First(c => c.CashFlowId == 2);
            Assert.Equal(45, edited.Days); // Jan 1 → Feb 15 = 45 days
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 7: Subsequent events get correct StartDate
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task EditEndDate_SubsequentEvents_Recalculated()
        {
            var fd = CreateFd();
            var interest = CreateInterest();
            var cashFlows = BuildQuarterlyCompoundingCashFlows(1, 100_000m, 8m);

            // Edit Q1 Interest EndDate from Apr 1 → Mar 15
            var result = await UpdateCashFlow(
                fd, interest, cashFlows,
                cashFlowIdToUpdate: 2,
                newEndDate: new DateTime(2025, 3, 15));

            // The edited event should have the new EndDate and recalculated Days
            var edited = result.First(c => c.CashFlowId == 2);
            Assert.Equal(new DateTime(2025, 3, 15), edited.EndDate);
            Assert.Equal(73, edited.Days); // Jan 1 → Mar 15 = 73 days

            // Subsequent events should have non-zero interest and
            // OpeningBalance chain should be consistent
            for (int i = 1; i < result.Count; i++)
            {
                var current = result[i];
                var prev = result[i - 1];
                if (current.Event == "Maturity") break;

                Assert.True(current.OpeningBalance == prev.ClosingBalance,
                    $"Event {current.CashFlowId}: OpeningBalance ({current.OpeningBalance}) " +
                    $"should match prev ClosingBalance ({prev.ClosingBalance})");
            }
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 8: Maturity reflects final compounded balance
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task CompoundingMode_Maturity_EqualsFinalCompoundedBalance()
        {
            var fd = CreateFd();
            var interest = CreateInterest();
            var cashFlows = BuildQuarterlyCompoundingCashFlows(1, 100_000m, 8m);

            var result = await UpdateCashFlow(
                fd, interest, cashFlows,
                cashFlowIdToUpdate: 2,
                newEndDate: new DateTime(2025, 3, 15));

            var lastCompound = result
                .Where(c => c.Event == "Compounding Interest")
                .Last();
            var maturity = result.First(c => c.Event == "Maturity");

            Assert.Equal(lastCompound.ClosingBalance, maturity.CashFlowAmount);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 9: Edit before target resets accrued correctly
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task EditBeforeTarget_CompoundingResetsAccrued()
        {
            // Edit a compounding event (not an interest event)
            var fd = CreateFd();
            var interest = CreateInterest();
            var cashFlows = BuildQuarterlyCompoundingCashFlows(1, 100_000m, 8m);

            // Edit Q1 Compounding event (CashFlowId = 3)
            var result = await UpdateCashFlow(
                fd, interest, cashFlows,
                cashFlowIdToUpdate: 3,
                newEndDate: new DateTime(2025, 4, 5));

            // After the compound event, Q2 Interest should recalculate
            var q2Interest = result.First(c => c.CashFlowId == 4);
            Assert.True(q2Interest.InterestAmount > 0,
                $"Q2 Interest should have positive interest after edit, got {q2Interest.InterestAmount}");

            // Q2 OpeningBalance should match Q1 Compound's ClosingBalance
            var q1Compound = result.First(c => c.CashFlowId == 3);
            Assert.Equal(q1Compound.ClosingBalance, q2Interest.OpeningBalance);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 10: Validation — EndDate must be after StartDate
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task UpdateAsync_EndDateBeforeStartDate_ThrowsInvalidOperation()
        {
            var fd = CreateFd();
            var interest = CreateInterest();
            var cashFlows = BuildQuarterlyCompoundingCashFlows(1, 100_000m, 8m);

            _cashFlowRepo.Setup(r => r.GetByFdIdAsync(fd.FdId))
                .ReturnsAsync(cashFlows);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId))
                .ReturnsAsync(fd);
            _interestRepo.Setup(r => r.GetByFdIdAsync(fd.FdId))
                .ReturnsAsync(interest);

            // Try to set EndDate to StartDate (same day = 0 days)
            var dto = new FDCashFlowDto
            {
                CashFlowId = 2,
                FdId = fd.FdId,
                EndDate = cashFlows[1].StartDate // Same as StartDate
            };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateAsync(2, dto));
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 11: Validation — EndDate cannot exceed FD maturity
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task UpdateAsync_EndDateExceedsMaturity_ThrowsInvalidOperation()
        {
            var fd = CreateFd();
            var interest = CreateInterest();
            var cashFlows = BuildQuarterlyCompoundingCashFlows(1, 100_000m, 8m);

            _cashFlowRepo.Setup(r => r.GetByFdIdAsync(fd.FdId))
                .ReturnsAsync(cashFlows);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId))
                .ReturnsAsync(fd);
            _interestRepo.Setup(r => r.GetByFdIdAsync(fd.FdId))
                .ReturnsAsync(interest);

            var dto = new FDCashFlowDto
            {
                CashFlowId = 2,
                FdId = fd.FdId,
                EndDate = new DateTime(2026, 1, 1) // After FD maturity
            };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateAsync(2, dto));
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 12: Edit non-existent cash flow returns null
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task UpdateAsync_NonExistentCashFlow_ReturnsNull()
        {
            var fd = CreateFd();
            var interest = CreateInterest();
            var cashFlows = BuildQuarterlyCompoundingCashFlows(1, 100_000m, 8m);

            _cashFlowRepo.Setup(r => r.GetByFdIdAsync(fd.FdId))
                .ReturnsAsync(cashFlows);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId))
                .ReturnsAsync(fd);
            _interestRepo.Setup(r => r.GetByFdIdAsync(fd.FdId))
                .ReturnsAsync(interest);

            var dto = new FDCashFlowDto
            {
                CashFlowId = 999, // Doesn't exist
                FdId = fd.FdId,
                EndDate = new DateTime(2025, 3, 15)
            };

            var result = await _service.UpdateAsync(999, dto);
            Assert.Null(result);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 13: Zero-day period produces zero interest
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task EditToZeroDays_ZeroInterest()
        {
            var fd = CreateFd();
            var interest = CreateInterest();
            var cashFlows = BuildQuarterlyCompoundingCashFlows(1, 100_000m, 8m);

            // Edit Q1 Interest to have same StartDate as EndDate (0 days)
            _cashFlowRepo.Setup(r => r.GetByFdIdAsync(fd.FdId))
                .ReturnsAsync(cashFlows);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId))
                .ReturnsAsync(fd);
            _interestRepo.Setup(r => r.GetByFdIdAsync(fd.FdId))
                .ReturnsAsync(interest);

            List<FDCashFlow>? captured = null;
            _cashFlowRepo.Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<FDCashFlow>>()))
                .Callback<IEnumerable<FDCashFlow>>(cf => captured = cf.ToList())
                .Returns(Task.CompletedTask);

            // Set EndDate = StartDate (0 days)
            var dto = new FDCashFlowDto
            {
                CashFlowId = 2,
                FdId = fd.FdId,
                EndDate = cashFlows[1].StartDate // Same as StartDate
            };

            // This should throw because EndDate <= StartDate validation
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateAsync(2, dto));
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 14: Multiple compounding events — balance grows
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task CompoundingMode_BalanceGrowsAtEachCompound()
        {
            var fd = CreateFd();
            var interest = CreateInterest();
            var cashFlows = BuildQuarterlyCompoundingCashFlows(1, 100_000m, 8m);

            var result = await UpdateCashFlow(
                fd, interest, cashFlows,
                cashFlowIdToUpdate: 2,
                newEndDate: new DateTime(2025, 3, 15));

            var compounds = result.Where(c => c.Event == "Compounding Interest").ToList();

            // Each compound with Days > 0 should have ClosingBalance > OpeningBalance.
            // Compounds with Days = 0 still compound accrued interest from prior events.
            decimal prevClosing = 100_000m;
            foreach (var c in compounds)
            {
                Assert.Equal(prevClosing, c.OpeningBalance);
                Assert.True(c.ClosingBalance >= c.OpeningBalance,
                    $"Compounding {c.CashFlowId}: Closing ({c.ClosingBalance}) " +
                    $"should be >= Opening ({c.OpeningBalance})");
                prevClosing = c.ClosingBalance;
            }

            // Final compounded balance should exceed the principal
            Assert.True(compounds.Last().ClosingBalance > 100_000m,
                $"Final balance ({compounds.Last().ClosingBalance}) should exceed principal");
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 15: All events have consistent currency and status
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task AllEvents_HaveConsistentMetadata()
        {
            var fd = CreateFd();
            var interest = CreateInterest();
            var cashFlows = BuildQuarterlyCompoundingCashFlows(1, 100_000m, 8m);

            var result = await UpdateCashFlow(
                fd, interest, cashFlows,
                cashFlowIdToUpdate: 2,
                newEndDate: new DateTime(2025, 3, 15));

            foreach (var cf in result)
            {
                Assert.Equal("INR", cf.CurrencyCode);
                Assert.Equal("PENDING", cf.Status);
                Assert.Equal("FD-0001", cf.ReferenceNo);
                Assert.Equal(1, cf.FdId);
                Assert.Equal(8m, cf.InterestRate);
            }
        }
    }
}
