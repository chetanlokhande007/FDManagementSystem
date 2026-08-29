using FinTrustFDManager.DAL.Repositories;
using FinTrustFDManager.DAL.Data;
using FinTrustFDManager.Model.Entities.Investment;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FinTrustFDManager.BAL.Tests.IntegrationTests
{
    /// <summary>
    /// Integration tests using EF Core InMemory provider.
    /// Tests the full EF Core pipeline: Repository → DbContext → InMemory store.
    /// The same ApplicationDbContext model is used (configured for PostgreSQL in production).
    /// </summary>
    public class RepositoryIntegrationTests : IDisposable
    {
        private readonly DatabaseFixture _fixture;
        private readonly ApplicationDbContext _context;

        public RepositoryIntegrationTests()
        {
            _fixture = new DatabaseFixture();
            _context = _fixture.Context;
        }

        public void Dispose()
        {
            _fixture?.Dispose();
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
                CurrencyId = 1,
                PrincipalAmount = principal,
                StartDate = start,
                EndDate = end,
                SettlementDate = end.AddDays(1),
                Status = "DRAFT"
            };
        }

        private static int MapFreq(string f) => f?.Trim().ToUpperInvariant().Replace("-", "_").Replace(" ", "_") switch
        {
            "MONTHLY" or "MONTH" => 1,
            "QUARTERLY" or "QUARTER" => 2,
            "HALF_YEARLY" or "HALFYEARLY" or "YEARLY" or "ANNUALLY" or "ANNUAL" or "YEAR" or "SEMI_ANNUAL" or "SEMIANNUAL" or "SEMI_ANNUALLY" or "SEMIANNUALLY" => 4,
            "AT_MATURITY" or "ATMATURITY" => 5,
            _ => 1
        };

        private static int MapDCC(string b) => b?.Trim().ToUpperInvariant().Replace("/", "_") switch
        {
            "30_360" => 1,
            "ACTUAL_360" => 2,
            "ACTUAL_365" => 3,
            "ACTUAL_ACTUAL" or "ACTUAL" => 4,
            _ => 3
        };

        private static FDInterest CreateInterest(
            long fdId = 1,
            decimal rate = 8m,
            string interestFreq = "QUARTERLY",
            bool isCompounding = true,
            string compoundingFreq = "QUARTERLY",
            string calcBasis = "ACTUAL_365")
        {
            return new FDInterest
            {
                FdId = fdId,
                InterestRateType = "FIXED",
                InterestRate = rate,
                InterestFrequencyId = MapFreq(interestFreq),
                CompoundingFrequencyId = string.Equals(compoundingFreq, "Not Applicable", StringComparison.OrdinalIgnoreCase) ? null : MapFreq(compoundingFreq),
                IsCompounding = isCompounding,
                DayCountConventionId = MapDCC(calcBasis),
                CreatedDate = DateTime.UtcNow
            };
        }

        private static FDCashFlow CreateCashFlow(
            long fdId,
            string eventType,
            decimal openingBalance,
            decimal interestAmount,
            decimal closingBalance,
            decimal cashFlowAmount,
            DateTime startDate,
            DateTime endDate,
            int days)
        {
            return new FDCashFlow
            {
                FdId = fdId,
                Event = eventType,
                StartDate = startDate,
                EndDate = endDate,
                Days = days,
                InterestRate = 8m,
                OpeningBalance = openingBalance,
                InterestAmount = interestAmount,
                ClosingBalance = closingBalance,
                CashFlowAmount = cashFlowAmount,
                Direction = eventType == "FD Created" ? "OUTFLOW" : "INFLOW",
                CurrencyCode = "INR",
                Status = "PENDING",
                ReferenceNo = "FD-0001",
                CreatedDate = DateTime.UtcNow
            };
        }

        // ═══════════════════════════════════════════════════════
        //  FDIdentification Repository Tests
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task FDIdentification_AddAsync_PersistsToDatabase()
        {
            var repo = new FDIdentificationRepository(_context);
            var fd = CreateFd(1);

            var result = await repo.AddAsync(fd);

            Assert.True(result.FdId > 0, "FD ID should be auto-generated");
            Assert.Equal("FD-0001", result.FdReferenceNo);

            var fromDb = await _context.FDIdentifications.FindAsync(result.FdId);
            Assert.NotNull(fromDb);
            Assert.Equal(100_000m, fromDb.PrincipalAmount);
            Assert.Equal(1, fromDb.CurrencyId);
        }

        [Fact]
        public async Task FDIdentification_GetAllAsync_ReturnsAllFDs()
        {
            var repo = new FDIdentificationRepository(_context);

            await repo.AddAsync(CreateFd(1, 100_000m));
            await repo.AddAsync(CreateFd(2, 200_000m));
            await repo.AddAsync(CreateFd(3, 300_000m));

            var all = (await repo.GetAllAsync()).ToList();

            Assert.Equal(3, all.Count);
        }

        [Fact]
        public async Task FDIdentification_GetByIdAsync_ReturnsCorrectFD()
        {
            var repo = new FDIdentificationRepository(_context);
            var fd1 = await repo.AddAsync(CreateFd(1, 100_000m));
            var fd2 = await repo.AddAsync(CreateFd(2, 200_000m));

            var result1 = await repo.GetByIdAsync(fd1.FdId);
            var result2 = await repo.GetByIdAsync(fd2.FdId);

            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.NotEqual(result1.FdId, result2.FdId);
            Assert.Equal(100_000m, result1.PrincipalAmount);
            Assert.Equal(200_000m, result2.PrincipalAmount);
        }

        [Fact]
        public async Task FDIdentification_GetByIdAsync_ReturnsNullForNonExistent()
        {
            var repo = new FDIdentificationRepository(_context);
            var fd = await repo.GetByIdAsync(999);
            Assert.Null(fd);
        }

        [Fact]
        public async Task FDIdentification_UpdateAsync_ModifiesExistingFD()
        {
            var repo = new FDIdentificationRepository(_context);
            var fd = await repo.AddAsync(CreateFd(1));

            fd.PrincipalAmount = 250_000m;
            fd.Status = "APPROVED";
            var updated = await repo.UpdateAsync(fd);

            Assert.NotNull(updated);
            Assert.Equal(250_000m, updated.PrincipalAmount);
            Assert.Equal("APPROVED", updated.Status);
            Assert.NotNull(updated.ModifiedDate);

            var fromDb = await _context.FDIdentifications.FindAsync(fd.FdId);
            Assert.Equal(250_000m, fromDb.PrincipalAmount);
        }

        [Fact]
        public async Task FDIdentification_DeleteAsync_RemovesFromDatabase()
        {
            var repo = new FDIdentificationRepository(_context);
            var fd = await repo.AddAsync(CreateFd(1));

            var deleted = await repo.DeleteAsync(fd.FdId);

            Assert.True(deleted);
            Assert.Null(await repo.GetByIdAsync(fd.FdId));
        }

        [Fact]
        public async Task FDIdentification_DeleteAsync_ReturnsFalseForNonExistent()
        {
            var repo = new FDIdentificationRepository(_context);
            var result = await repo.DeleteAsync(999);
            Assert.False(result);
        }

        [Fact]
        public async Task FDIdentification_ChangeStatusAsync_UpdatesStatus()
        {
            var repo = new FDIdentificationRepository(_context);
            var fd = await repo.AddAsync(CreateFd(1));

            var result = await repo.ChangeStatusAsync(fd.FdId, "APPROVED");

            Assert.True(result);
            var fromDb = await repo.GetByIdAsync(fd.FdId);
            Assert.Equal("APPROVED", fromDb.Status);
        }

        [Fact]
        public async Task FDIdentification_GetLastAsync_ReturnsHighestId()
        {
            var repo = new FDIdentificationRepository(_context);
            await repo.AddAsync(CreateFd(1));
            await repo.AddAsync(CreateFd(3));
            await repo.AddAsync(CreateFd(2));

            var last = await repo.GetLastAsync();

            Assert.NotNull(last);
            Assert.Equal(3, last.FdId);
        }

        // ═══════════════════════════════════════════════════════
        //  FDInterest Repository Tests
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task FDInterest_AddAsync_PersistsToDatabase()
        {
            var fdRepo = new FDIdentificationRepository(_context);
            var intRepo = new FDInterestRepository(_context);

            var fd = await fdRepo.AddAsync(CreateFd(1));
            var interest = await intRepo.AddAsync(CreateInterest(fd.FdId));

            Assert.True(interest.FdInterestId > 0);
            Assert.Equal(fd.FdId, interest.FdId);
            Assert.Equal(8m, interest.InterestRate);

            var fromDb = await _context.FDInterests.FindAsync(interest.FdInterestId);
            Assert.NotNull(fromDb);
            Assert.Equal(2, fromDb.InterestFrequencyId);
        }

        [Fact]
        public async Task FDInterest_GetByFdIdAsync_ReturnsCorrectInterest()
        {
            var fdRepo = new FDIdentificationRepository(_context);
            var intRepo = new FDInterestRepository(_context);

            var fd1 = await fdRepo.AddAsync(CreateFd(1));
            var fd2 = await fdRepo.AddAsync(CreateFd(2));
            await intRepo.AddAsync(CreateInterest(fd1.FdId, 6m));
            await intRepo.AddAsync(CreateInterest(fd2.FdId, 10m));

            var interest = await intRepo.GetByFdIdAsync(fd2.FdId);

            Assert.NotNull(interest);
            Assert.Equal(10m, interest.InterestRate);
        }

        [Fact]
        public async Task FDInterest_GetByFdIdAsync_ReturnsNullForNoInterest()
        {
            var intRepo = new FDInterestRepository(_context);
            var interest = await intRepo.GetByFdIdAsync(999);
            Assert.Null(interest);
        }

        [Fact]
        public async Task FDInterest_UpdateAsync_ModifiesExistingInterest()
        {
            var fdRepo = new FDIdentificationRepository(_context);
            var intRepo = new FDInterestRepository(_context);

            var fd = await fdRepo.AddAsync(CreateFd(1));
            var interest = await intRepo.AddAsync(CreateInterest(fd.FdId));

            interest.InterestRate = 12m;
            interest.IsCompounding = false;
            var updated = await intRepo.UpdateAsync(interest);

            Assert.NotNull(updated);
            Assert.Equal(12m, updated.InterestRate);
            Assert.False(updated.IsCompounding);
        }

        [Fact]
        public async Task FDInterest_DeleteAsync_RemovesFromDatabase()
        {
            var fdRepo = new FDIdentificationRepository(_context);
            var intRepo = new FDInterestRepository(_context);

            var fd = await fdRepo.AddAsync(CreateFd(1));
            var interest = await intRepo.AddAsync(CreateInterest(fd.FdId));

            var deleted = await intRepo.DeleteAsync(interest.FdInterestId);

            Assert.True(deleted);
            Assert.Null(await intRepo.GetByIdAsync(interest.FdInterestId));
        }

        // ═══════════════════════════════════════════════════════
        //  FDCashFlow Repository Tests
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task FDCashFlow_AddRangeAsync_PersistsMultipleCashFlows()
        {
            var fdRepo = new FDIdentificationRepository(_context);
            var cfRepo = new FDCashFlowRepository(_context);

            var fd = await fdRepo.AddAsync(CreateFd(1));

            var cashFlows = new List<FDCashFlow>
            {
                CreateCashFlow(fd.FdId, "FD Created", 0, 0, 100_000m, 100_000m,
                    new DateTime(2025, 1, 1), new DateTime(2025, 1, 1), 0),
                CreateCashFlow(fd.FdId, "Interest", 100_000m, 1972.60m, 100_000m, 1972.60m,
                    new DateTime(2025, 1, 1), new DateTime(2025, 4, 1), 90),
                CreateCashFlow(fd.FdId, "Maturity", 100_000m, 0, 0, 100_000m,
                    new DateTime(2025, 12, 31), new DateTime(2025, 12, 31), 0)
            };

            await cfRepo.AddRangeAsync(cashFlows);

            var fromDb = (await cfRepo.GetByFdIdAsync(fd.FdId)).ToList();
            Assert.Equal(3, fromDb.Count);
        }

        [Fact]
        public async Task FDCashFlow_GetByFdIdAsync_ReturnsOnlyMatchingFDs()
        {
            var fdRepo = new FDIdentificationRepository(_context);
            var cfRepo = new FDCashFlowRepository(_context);

            var fd1 = await fdRepo.AddAsync(CreateFd(1));
            var fd2 = await fdRepo.AddAsync(CreateFd(2));

            await cfRepo.AddRangeAsync(new[]
            {
                CreateCashFlow(fd1.FdId, "FD Created", 0, 0, 100_000m, 100_000m,
                    DateTime.Today, DateTime.Today, 0),
                CreateCashFlow(fd2.FdId, "FD Created", 0, 0, 200_000m, 200_000m,
                    DateTime.Today, DateTime.Today, 0)
            });

            var cf1 = (await cfRepo.GetByFdIdAsync(fd1.FdId)).ToList();
            var cf2 = (await cfRepo.GetByFdIdAsync(fd2.FdId)).ToList();

            Assert.Single(cf1);
            Assert.Equal(100_000m, cf1[0].CashFlowAmount);

            Assert.Single(cf2);
            Assert.Equal(200_000m, cf2[0].CashFlowAmount);
        }

        [Fact]
        public async Task FDCashFlow_DeleteRangeAsync_RemovesAllCashFlowsForFD()
        {
            var fdRepo = new FDIdentificationRepository(_context);
            var cfRepo = new FDCashFlowRepository(_context);

            var fd = await fdRepo.AddAsync(CreateFd(1));
            var cashFlows = new List<FDCashFlow>
            {
                CreateCashFlow(fd.FdId, "FD Created", 0, 0, 100_000m, 100_000m,
                    DateTime.Today, DateTime.Today, 0),
                CreateCashFlow(fd.FdId, "Interest", 100_000m, 1000m, 100_000m, 1000m,
                    DateTime.Today, DateTime.Today, 30)
            };
            await cfRepo.AddRangeAsync(cashFlows);

            // Use a fresh context to avoid tracking conflicts
            using var freshContext = _fixture.CreateFreshContext();
            var freshCfRepo = new FDCashFlowRepository(freshContext);
            var toDelete = (await freshCfRepo.GetByFdIdAsync(fd.FdId)).ToList();
            await freshCfRepo.DeleteRangeAsync(toDelete);

            var remaining = (await freshCfRepo.GetByFdIdAsync(fd.FdId)).ToList();
            Assert.Empty(remaining);
        }

        [Fact]
        public async Task FDCashFlow_UpdateRangeAsync_ModifiesMultipleCashFlows()
        {
            var fdRepo = new FDIdentificationRepository(_context);
            var cfRepo = new FDCashFlowRepository(_context);

            var fd = await fdRepo.AddAsync(CreateFd(1));
            var cashFlows = new List<FDCashFlow>
            {
                CreateCashFlow(fd.FdId, "FD Created", 0, 0, 100_000m, 100_000m,
                    DateTime.Today, DateTime.Today, 0),
                CreateCashFlow(fd.FdId, "Interest", 100_000m, 1000m, 100_000m, 1000m,
                    DateTime.Today, DateTime.Today, 30)
            };
            await cfRepo.AddRangeAsync(cashFlows);

            // Use a fresh context to avoid tracking conflicts
            using var freshContext = _fixture.CreateFreshContext();
            var freshCfRepo = new FDCashFlowRepository(freshContext);

            var fromDb = (await freshCfRepo.GetByFdIdAsync(fd.FdId)).ToList();
            var interestCf = fromDb.First(c => c.Event == "Interest");
            interestCf.InterestAmount = 1500m;
            interestCf.CashFlowAmount = 1500m;

            await freshCfRepo.UpdateRangeAsync(fromDb);

            var updated = (await freshCfRepo.GetByFdIdAsync(fd.FdId)).ToList();
            var updatedInterest = updated.First(c => c.Event == "Interest");
            Assert.Equal(1500m, updatedInterest.InterestAmount);
        }

        // ═══════════════════════════════════════════════════════
        //  Cascade Delete Tests
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task CascadeDelete_DeletingFD_RemovesInterestAndCashFlows()
        {
            var fdRepo = new FDIdentificationRepository(_context);
            var intRepo = new FDInterestRepository(_context);
            var cfRepo = new FDCashFlowRepository(_context);

            var fd = await fdRepo.AddAsync(CreateFd(1));
            await intRepo.AddAsync(CreateInterest(fd.FdId));
            await cfRepo.AddRangeAsync(new[]
            {
                CreateCashFlow(fd.FdId, "FD Created", 0, 0, 100_000m, 100_000m,
                    DateTime.Today, DateTime.Today, 0),
                CreateCashFlow(fd.FdId, "Interest", 100_000m, 1000m, 100_000m, 1000m,
                    DateTime.Today, DateTime.Today, 30)
            });

            // Delete FD (cascade should remove interest and cash flows)
            await fdRepo.DeleteAsync(fd.FdId);

            // Verify cascade
            Assert.Null(await fdRepo.GetByIdAsync(fd.FdId));
            Assert.Null(await intRepo.GetByFdIdAsync(fd.FdId));
            Assert.Empty(await cfRepo.GetByFdIdAsync(fd.FdId));
        }

        // ═══════════════════════════════════════════════════════
        //  Landing Data Query Tests
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task GetLandingDataAsync_ReturnsFDsWithInterest()
        {
            var fdRepo = new FDIdentificationRepository(_context);
            var intRepo = new FDInterestRepository(_context);

            var fd = await fdRepo.AddAsync(CreateFd(1, 100_000m));
            await intRepo.AddAsync(CreateInterest(fd.FdId, 8m));

            var landing = (await fdRepo.GetLandingDataAsync()).ToList();

            Assert.Single(landing);
            var data = landing[0];
            Assert.Equal(fd.FdId, data.FdId);
            Assert.Equal(100_000m, data.PrincipalAmount);
            Assert.Equal(8m, data.InterestRate);
            Assert.Equal("QUARTERLY", data.InterestFrequency);
            Assert.Equal("ACTUAL_365", data.CalculationBasis);
        }

        [Fact]
        public async Task GetLandingDataAsync_FDsWithoutInterest_HaveZeroInterest()
        {
            var fdRepo = new FDIdentificationRepository(_context);

            var fd = await fdRepo.AddAsync(CreateFd(1, 100_000m));

            var landing = (await fdRepo.GetLandingDataAsync()).ToList();

            Assert.Single(landing);
            Assert.Equal(0m, landing[0].InterestRate);
            Assert.Equal("Not Applicable", landing[0].CompoundingFrequency);
            Assert.Equal(100_000m, landing[0].TotalAmount);
        }

        [Fact]
        public async Task GetLandingDataAsync_MultipleFDs_ReturnsAll()
        {
            var fdRepo = new FDIdentificationRepository(_context);
            var intRepo = new FDInterestRepository(_context);

            var fd1 = await fdRepo.AddAsync(CreateFd(1, 100_000m));
            var fd2 = await fdRepo.AddAsync(CreateFd(2, 200_000m));
            await intRepo.AddAsync(CreateInterest(fd1.FdId, 8m));
            await intRepo.AddAsync(CreateInterest(fd2.FdId, 10m));

            var landing = (await fdRepo.GetLandingDataAsync()).ToList();

            Assert.Equal(2, landing.Count);
            Assert.Contains(landing, l => l.FdId == fd1.FdId && l.InterestRate == 8m);
            Assert.Contains(landing, l => l.FdId == fd2.FdId && l.InterestRate == 10m);
        }

        [Fact]
        public async Task GetLandingDataAsync_CompoundingFD_ShowsCompoundingFrequency()
        {
            var fdRepo = new FDIdentificationRepository(_context);
            var intRepo = new FDInterestRepository(_context);

            var fd = await fdRepo.AddAsync(CreateFd(1, 100_000m));
            await intRepo.AddAsync(CreateInterest(fd.FdId, 8m, isCompounding: true, compoundingFreq: "QUARTERLY"));

            var landing = (await fdRepo.GetLandingDataAsync()).ToList();

            Assert.Single(landing);
            Assert.Equal("QUARTERLY", landing[0].CompoundingFrequency);
        }

        [Fact]
        public async Task GetLandingDataAsync_NonCompoundingFD_ShowsNotApplicable()
        {
            var fdRepo = new FDIdentificationRepository(_context);
            var intRepo = new FDInterestRepository(_context);

            var fd = await fdRepo.AddAsync(CreateFd(1, 100_000m));
            await intRepo.AddAsync(CreateInterest(fd.FdId, 8m, isCompounding: false));

            var landing = (await fdRepo.GetLandingDataAsync()).ToList();

            Assert.Single(landing);
            Assert.Equal("Not Applicable", landing[0].CompoundingFrequency);
        }

        // ═══════════════════════════════════════════════════════
        //  UnitOfWork Transaction Tests
        // ═══════════════════════════════════════════════════════

        // NOTE: EF Core InMemory provider does not support real transactions.
        // These tests verify UnitOfWork behavior against PostgreSQL in production.

        [Fact(Skip = "InMemory provider does not support transactions; requires PostgreSQL")]
        public async Task UnitOfWork_CommitTransaction_PersistsChanges()
        {
            var uow = new UnitOfWork(_context);
            var fdRepo = new FDIdentificationRepository(_context);

            await using var transaction = await uow.BeginTransactionAsync();

            _context.FDIdentifications.Add(CreateFd(1));
            await uow.CommitTransactionAsync();

            var fromDb = await fdRepo.GetByIdAsync(1);
            Assert.NotNull(fromDb);
        }

        [Fact(Skip = "InMemory provider does not support transactions; requires PostgreSQL")]
        public async Task UnitOfWork_RollbackTransaction_DiscardsChanges()
        {
            var uow = new UnitOfWork(_context);
            var fdRepo = new FDIdentificationRepository(_context);

            await using var transaction = await uow.BeginTransactionAsync();

            _context.FDIdentifications.Add(CreateFd(1));
            await uow.RollbackTransactionAsync();

            var fromDb = await fdRepo.GetByIdAsync(1);
            Assert.Null(fromDb);
        }
    }
}
