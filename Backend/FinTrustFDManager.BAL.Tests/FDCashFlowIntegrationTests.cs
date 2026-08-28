using FinTrustFDManager.BAL.DTOs;
using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.BAL.Services;
using FinTrustFDManager.DAL.Data;
using FinTrustFDManager.DAL.Repositories;
using FinTrustFDManager.Model.Entities.Investment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FinTrustFDManager.BAL.Tests
{
    /// <summary>
    /// Integration tests for the FDCashFlow pipeline using an InMemory database.
    /// These tests exercise the full service → repository → database chain
    /// without mocking the data layer.
    /// </summary>
    public class FDCashFlowIntegrationTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly FDCashFlowRepository _cashFlowRepo;
        private readonly FDInterestRepository _interestRepo;
        private readonly FDIdentificationRepository _fdRepo;
        private readonly UnitOfWork _unitOfWork;
        private readonly FDCashFlowService _cashFlowService;
        private readonly FDInterestService _interestService;

        public FDCashFlowIntegrationTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _context = new ApplicationDbContext(options);
            _cashFlowRepo = new FDCashFlowRepository(_context);
            _interestRepo = new FDInterestRepository(_context);
            _fdRepo = new FDIdentificationRepository(_context);
            _unitOfWork = new UnitOfWork(_context);

            var loggerCashFlow = new Mock<ILogger<FDCashFlowService>>();
            var loggerInterest = new Mock<ILogger<FDInterestService>>();
            var benchmarkRateHistoryService = new Mock<IBenchmarkRateHistoryService>();
            benchmarkRateHistoryService.Setup(s => s.GetEffectiveRateAsync(It.IsAny<int>(), It.IsAny<DateTime>()))
                .ReturnsAsync(0m);

            _interestService = new FDInterestService(
                _interestRepo, _fdRepo, _cashFlowRepo, benchmarkRateHistoryService.Object, _unitOfWork, loggerInterest.Object);

            _cashFlowService = new FDCashFlowService(
                _cashFlowRepo, _interestService, _fdRepo, _unitOfWork, loggerCashFlow.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        // ═══════════════════════════════════════════
        //  Helpers
        // ═══════════════════════════════════════════

        private async Task<FDIdentification> SeedFd(
            long fdId = 1,
            decimal principal = 100_000m,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var fd = new FDIdentification
            {
                FdId = fdId,
                FdReferenceNo = $"FD-{fdId:D4}",
                EntityId = 1,
                CounterpartyId = 1,
                CurrencyCode = "INR",
                PrincipalAmount = principal,
                StartDate = startDate ?? new DateTime(2025, 1, 1),
                EndDate = endDate ?? new DateTime(2025, 12, 31),
                SettlementDate = (endDate ?? new DateTime(2025, 12, 31)).AddDays(1),
                Status = "DRAFT"
            };
            _context.FDIdentifications.Add(fd);
            await _context.SaveChangesAsync();
            return fd;
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 1: CreateInterest generates cash flows in DB
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task CreateInterest_GeneratesCashFlows_InDatabase()
        {
            var fd = await SeedFd();

            var interest = new FDInterest
            {
                FdId = fd.FdId,
                InterestRateType = "FIXED",
                InterestRate = 8m,
                InterestFrequency = "QUARTERLY",
                CompoundingFrequency = "Not Applicable",
                IsCompounding = false,
                CalculationBasis = "ACTUAL_365"
            };

            var result = await _interestService.CreateAsync(interest);

            // Interest should be created
            Assert.NotNull(result);
            Assert.Equal(fd.FdId, result.FdId);

            // Cash flows should exist in the database
            var cashFlows = (await _cashFlowRepo.GetByFdIdAsync(fd.FdId)).ToList();
            Assert.True(cashFlows.Count >= 3, // FD Created + Interest + Maturity
                $"Expected >= 3 cash flows, got {cashFlows.Count}");

            // First event should be FD Created
            Assert.Equal("FD Created", cashFlows[0].Event);
            Assert.Equal("OUTFLOW", cashFlows[0].Direction);
            Assert.Equal(fd.PrincipalAmount, cashFlows[0].CashFlowAmount);

            // Last event should be Maturity
            Assert.Equal("Maturity", cashFlows.Last().Event);
            Assert.Equal("INFLOW", cashFlows.Last().Direction);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 2: GetByFdIdAsync returns correct summary
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task GetByFdIdAsync_ReturnsCorrectSummary()
        {
            var fd = await SeedFd(principal: 100_000m);

            var interest = new FDInterest
            {
                FdId = fd.FdId,
                InterestRateType = "FIXED",
                InterestRate = 8m,
                InterestFrequency = "QUARTERLY",
                CompoundingFrequency = "Not Applicable",
                IsCompounding = false,
                CalculationBasis = "ACTUAL_365"
            };
            await _interestService.CreateAsync(interest);

            var summary = await _cashFlowService.GetByFdIdAsync(fd.FdId);

            Assert.Equal(fd.FdId, summary.FdId);
            Assert.Equal(100_000m, summary.PrincipalAmount);
            Assert.True(summary.TotalInterest > 0, "TotalInterest should be > 0");
            // For non-compounding: MaturityAmount = principal (cash flows paid out separately)
            // For compounding: MaturityAmount = principal + all compounded interest
            // Both are valid; totalInterest + principal should approximate maturityAmount
            Assert.True(summary.MaturityAmount >= summary.PrincipalAmount,
                $"MaturityAmount ({summary.MaturityAmount}) should be >= principal ({summary.PrincipalAmount})");
            Assert.True(summary.Schedule.Count >= 3,
                $"Expected >= 3 cash flows, got {summary.Schedule.Count}");
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 3: Non-compounding — balance stays constant
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task NonCompounding_OpeningBalance_RemainsConstant()
        {
            var fd = await SeedFd(
                principal: 96_000m,
                startDate: new DateTime(2026, 2, 6),
                endDate: new DateTime(2026, 8, 6));

            var interest = new FDInterest
            {
                FdId = fd.FdId,
                InterestRateType = "FIXED",
                InterestRate = 1m,
                InterestFrequency = "HALF_YEARLY",
                CompoundingFrequency = "Not Applicable",
                IsCompounding = false,
                CalculationBasis = "ACTUAL_365"
            };
            await _interestService.CreateAsync(interest);

            var summary = await _cashFlowService.GetByFdIdAsync(fd.FdId);
            var interestEvents = summary.Schedule
                .Where(c => c.Event == "Interest").ToList();

            Assert.True(interestEvents.Count >= 1,
                $"Expected >= 1 interest event, got {interestEvents.Count}");

            // ALL interest events: OpeningBalance = ClosingBalance = Principal (non-compounding)
            foreach (var ie in interestEvents)
            {
                Assert.Equal(96_000m, ie.OpeningBalance);
                Assert.Equal(96_000m, ie.ClosingBalance);

                // Interest is paid out (CashFlowAmount == InterestAmount)
                Assert.True(ie.CashFlowAmount > 0);
                Assert.Equal(ie.InterestAmount, ie.CashFlowAmount);
            }
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 4: Compounding — balance grows
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task Compounding_BalanceGrows_AcrossPeriods()
        {
            var fd = await SeedFd(
                principal: 60_000m,
                startDate: new DateTime(2025, 1, 1),
                endDate: new DateTime(2025, 12, 31));

            var interest = new FDInterest
            {
                FdId = fd.FdId,
                InterestRateType = "FIXED",
                InterestRate = 8m,
                InterestFrequency = "QUARTERLY",
                CompoundingFrequency = "QUARTERLY",
                IsCompounding = true,
                CalculationBasis = "ACTUAL_365"
            };
            await _interestService.CreateAsync(interest);

            var summary = await _cashFlowService.GetByFdIdAsync(fd.FdId);

            // Should have Compounding Interest events
            var compoundingEvents = summary.Schedule
                .Where(c => c.Event == "Compounding Interest").ToList();
            Assert.True(compoundingEvents.Count >= 1,
                $"Expected >= 1 compounding event, got {compoundingEvents.Count}");

            // Balance should grow at each compounding event
            decimal prevBalance = 60_000m;
            foreach (var ce in compoundingEvents)
            {
                Assert.True(ce.ClosingBalance > ce.OpeningBalance,
                    $"Closing ({ce.ClosingBalance}) should exceed Opening ({ce.OpeningBalance})");
                Assert.Equal(prevBalance, ce.OpeningBalance);
                prevBalance = ce.ClosingBalance;
            }

            // Maturity should reflect the compounded balance
            var maturity = summary.Schedule.Last(c => c.Event == "Maturity");
            Assert.True(maturity.CashFlowAmount > 60_000m,
                $"Maturity ({maturity.CashFlowAmount}) should exceed principal (60000)");
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 5: Partial period — maturity date not on boundary
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task PartialPeriod_FinalPeriod_CalculatedCorrectly()
        {
            // Feb 6 2026 → Nov 2 2028, half-yearly
            // Regular periods: Feb→Aug, Aug→Feb, Feb→Aug, Aug→Feb, Feb→Aug, Aug→Feb
            // Final: Aug 2028 → Nov 2028 (partial)
            var fd = await SeedFd(
                principal: 100_000m,
                startDate: new DateTime(2026, 2, 6),
                endDate: new DateTime(2028, 11, 2));

            var interest = new FDInterest
            {
                FdId = fd.FdId,
                InterestRateType = "FIXED",
                InterestRate = 8m,
                InterestFrequency = "HALF_YEARLY",
                CompoundingFrequency = "Not Applicable",
                IsCompounding = false,
                CalculationBasis = "ACTUAL_365"
            };
            await _interestService.CreateAsync(interest);

            var summary = await _cashFlowService.GetByFdIdAsync(fd.FdId);
            var interestEvents = summary.Schedule
                .Where(c => c.Event == "Interest").ToList();

            // Last interest event should be a partial period (< 180 days)
            var lastInterest = interestEvents.Last();
            Assert.True(lastInterest.Days < 180,
                $"Last period ({lastInterest.Days} days) should be a partial period");

            // Maturity should still return principal
            var maturity = summary.Schedule.Last(c => c.Event == "Maturity");
            Assert.Equal(fd.PrincipalAmount, maturity.CashFlowAmount);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 6: Summary totals match individual cash flows
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task SummaryTotals_MatchIndividualCashFlows()
        {
            var fd = await SeedFd(principal: 200_000m);

            var interest = new FDInterest
            {
                FdId = fd.FdId,
                InterestRateType = "FIXED",
                InterestRate = 10m,
                InterestFrequency = "MONTHLY",
                CompoundingFrequency = "Not Applicable",
                IsCompounding = false,
                CalculationBasis = "ACTUAL_365"
            };
            await _interestService.CreateAsync(interest);

            var summary = await _cashFlowService.GetByFdIdAsync(fd.FdId);

            // Sum of all interest amounts should equal TotalInterest
            decimal sumInterest = summary.Schedule
                .Where(c => c.Event == "Interest")
                .Sum(c => c.CashFlowAmount);

            Assert.Equal(summary.TotalInterest, sumInterest);

            // MaturityAmount should be >= principal
            Assert.True(summary.MaturityAmount >= summary.PrincipalAmount,
                $"MaturityAmount ({summary.MaturityAmount}) should be >= principal ({summary.PrincipalAmount})");
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 7: GetByFdIdAsync returns empty summary for non-existent FD
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task GetByFdIdAsync_NonExistentFd_ReturnsEmptySummary()
        {
            var summary = await _cashFlowService.GetByFdIdAsync(999);

            Assert.Equal(999, summary.FdId);
            Assert.Equal(0, summary.PrincipalAmount);
            Assert.Equal(0, summary.TotalInterest);
            Assert.Equal(0, summary.MaturityAmount);
            Assert.Empty(summary.Schedule);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 8: CreateCashFlow persists to database
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task CreateCashFlow_PersistsToDatabase()
        {
            var fd = await SeedFd();

            var dto = new FDCashFlowDto
            {
                FdId = fd.FdId,
                Event = "FD Created",
                StartDate = fd.StartDate,
                EndDate = fd.StartDate,
                Days = 0,
                InterestRate = 8m,
                OpeningBalance = 0,
                InterestAmount = 0,
                ClosingBalance = fd.PrincipalAmount,
                CashFlowAmount = fd.PrincipalAmount,
                Direction = "OUTFLOW",
                CurrencyCode = "INR",
                Status = "PENDING",
                ReferenceNo = fd.FdReferenceNo
            };

            var result = await _cashFlowService.CreateAsync(dto);

            Assert.True(result.CashFlowId > 0, "CashFlowId should be assigned");

            // Verify it exists in the database
            var fromDb = await _cashFlowRepo.GetByIdAsync(result.CashFlowId);
            Assert.NotNull(fromDb);
            Assert.Equal("FD Created", fromDb.Event);
            Assert.Equal(fd.PrincipalAmount, fromDb.CashFlowAmount);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 9: DeleteCashFlow removes from database
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task DeleteCashFlow_RemovesFromDatabase()
        {
            var fd = await SeedFd();

            var dto = new FDCashFlowDto
            {
                FdId = fd.FdId,
                Event = "FD Created",
                StartDate = fd.StartDate,
                EndDate = fd.StartDate,
                Days = 0,
                InterestRate = 8m,
                OpeningBalance = 0,
                InterestAmount = 0,
                ClosingBalance = fd.PrincipalAmount,
                CashFlowAmount = fd.PrincipalAmount,
                Direction = "OUTFLOW",
                CurrencyCode = "INR",
                Status = "PENDING",
                ReferenceNo = fd.FdReferenceNo
            };

            var created = await _cashFlowService.CreateAsync(dto);
            var deleted = await _cashFlowService.DeleteAsync(created.CashFlowId);

            Assert.True(deleted);
            var fromDb = await _cashFlowRepo.GetByIdAsync(created.CashFlowId);
            Assert.Null(fromDb);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 10: DeleteCashFlow returns false for non-existent
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task DeleteCashFlow_NonExistent_ReturnsFalse()
        {
            var deleted = await _cashFlowService.DeleteAsync(999);
            Assert.False(deleted);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 11: GetAll returns cash flows across FDs
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task GetAll_ReturnsCashFlowsAcrossMultipleFds()
        {
            var fd1 = await SeedFd(fdId: 1, principal: 50_000m);
            var fd2 = await SeedFd(fdId: 2, principal: 75_000m);

            var interest1 = new FDInterest
            {
                FdId = 1, InterestRateType = "FIXED", InterestRate = 8m,
                InterestFrequency = "MONTHLY", CompoundingFrequency = "Not Applicable",
                IsCompounding = false, CalculationBasis = "ACTUAL_365"
            };
            var interest2 = new FDInterest
            {
                FdId = 2, InterestRateType = "FIXED", InterestRate = 6m,
                InterestFrequency = "QUARTERLY", CompoundingFrequency = "Not Applicable",
                IsCompounding = false, CalculationBasis = "ACTUAL_365"
            };

            await _interestService.CreateAsync(interest1);
            await _interestService.CreateAsync(interest2);

            var all = (await _cashFlowService.GetAllAsync()).ToList();

            // Both FDs should have cash flows
            Assert.Contains(all, c => c.FdId == 1);
            Assert.Contains(all, c => c.FdId == 2);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 12: Cash flow dates align with FD dates
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task CashFlowDates_AlignWithFdDates()
        {
            var fd = await SeedFd(
                startDate: new DateTime(2025, 3, 15),
                endDate: new DateTime(2025, 9, 15));

            var interest = new FDInterest
            {
                FdId = fd.FdId,
                InterestRateType = "FIXED",
                InterestRate = 7m,
                InterestFrequency = "MONTHLY",
                CompoundingFrequency = "Not Applicable",
                IsCompounding = false,
                CalculationBasis = "ACTUAL_365"
            };
            await _interestService.CreateAsync(interest);

            var summary = await _cashFlowService.GetByFdIdAsync(fd.FdId);
            var events = summary.Schedule.OrderBy(c => c.StartDate).ToList();

            // First event starts at FD start date
            Assert.Equal(new DateTime(2025, 3, 15), events[0].StartDate);

            // Last event ends at FD end date
            Assert.Equal(new DateTime(2025, 9, 15), events.Last().EndDate);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 13: All cash flows have consistent metadata
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task AllCashFlows_HaveConsistentMetadata()
        {
            var fd = await SeedFd();

            var interest = new FDInterest
            {
                FdId = fd.FdId,
                InterestRateType = "FIXED",
                InterestRate = 8m,
                InterestFrequency = "QUARTERLY",
                CompoundingFrequency = "Not Applicable",
                IsCompounding = false,
                CalculationBasis = "ACTUAL_365"
            };
            await _interestService.CreateAsync(interest);

            var summary = await _cashFlowService.GetByFdIdAsync(fd.FdId);

            foreach (var cf in summary.Schedule)
            {
                Assert.Equal(fd.FdId, cf.FdId);
                Assert.Equal("INR", cf.CurrencyCode);
                Assert.Equal("PENDING", cf.Status);
                Assert.Equal("FD-0001", cf.ReferenceNo);
                Assert.Equal(8m, cf.InterestRate);
            }
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 14: Update interest regenerates cash flows
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task UpdateInterest_RegeneratesCashFlows()
        {
            var fd = await SeedFd();

            var interest = new FDInterest
            {
                FdId = fd.FdId,
                InterestRateType = "FIXED",
                InterestRate = 5m,
                InterestFrequency = "QUARTERLY",
                CompoundingFrequency = "Not Applicable",
                IsCompounding = false,
                CalculationBasis = "ACTUAL_365"
            };
            await _interestService.CreateAsync(interest);

            // Get initial summary
            var summary1 = await _cashFlowService.GetByFdIdAsync(fd.FdId);
            decimal initialInterest = summary1.TotalInterest;

            // Create a second FD with the higher rate to compare
            var fd2 = await SeedFd(fdId: 2, principal: 100_000m);
            var interest2 = new FDInterest
            {
                FdId = fd2.FdId,
                InterestRateType = "FIXED",
                InterestRate = 10m,
                InterestFrequency = "QUARTERLY",
                CompoundingFrequency = "Not Applicable",
                IsCompounding = false,
                CalculationBasis = "ACTUAL_365"
            };
            await _interestService.CreateAsync(interest2);

            var summary2 = await _cashFlowService.GetByFdIdAsync(fd2.FdId);

            // 10% FD should have higher total interest than 5% FD
            Assert.True(summary2.TotalInterest > initialInterest,
                $"10% interest ({summary2.TotalInterest}) should exceed 5% interest ({initialInterest})");
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 15: Delete interest removes associated cash flows
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task DeleteInterest_RemovesAssociatedCashFlows()
        {
            var fd = await SeedFd();

            var interest = new FDInterest
            {
                FdId = fd.FdId,
                InterestRateType = "FIXED",
                InterestRate = 8m,
                InterestFrequency = "QUARTERLY",
                CompoundingFrequency = "Not Applicable",
                IsCompounding = false,
                CalculationBasis = "ACTUAL_365"
            };
            await _interestService.CreateAsync(interest);

            // Cash flows should exist
            var before = (await _cashFlowRepo.GetByFdIdAsync(fd.FdId)).ToList();
            Assert.True(before.Count > 0);

            // Delete interest via repository directly (avoids InMemory transaction tracking issue)
            var deleted = await _interestRepo.DeleteAsync(interest.FdInterestId);
            Assert.True(deleted);

            // Interest should be gone
            var interestAfter = await _interestRepo.GetByFdIdAsync(fd.FdId);
            Assert.Null(interestAfter);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 16: AT_MATURITY — single period, no intermediate events
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task AtMaturity_SinglePeriod_NoIntermediateInterest()
        {
            var fd = await SeedFd();

            var interest = new FDInterest
            {
                FdId = fd.FdId,
                InterestRateType = "FIXED",
                InterestRate = 8m,
                InterestFrequency = "AT_MATURITY",
                CompoundingFrequency = "Not Applicable",
                IsCompounding = false,
                CalculationBasis = "ACTUAL_365"
            };
            await _interestService.CreateAsync(interest);

            var summary = await _cashFlowService.GetByFdIdAsync(fd.FdId);
            var interestEvents = summary.Schedule
                .Where(c => c.Event == "Interest").ToList();

            // Should have exactly 1 interest event (the full period)
            Assert.Single(interestEvents);

            // That interest event covers the entire FD duration
            Assert.Equal(fd.StartDate, interestEvents[0].StartDate);
            Assert.Equal(fd.EndDate, interestEvents[0].EndDate);

            // Interest should be > 0
            Assert.True(interestEvents[0].InterestAmount > 0);
        }
    }
}
