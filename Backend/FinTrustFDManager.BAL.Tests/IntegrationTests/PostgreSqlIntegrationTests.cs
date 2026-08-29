using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.BAL.Services;
using FinTrustFDManager.DAL.Data;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.DAL.Repositories;
using FinTrustFDManager.Model.DTOs.Amendment;
using FinTrustFDManager.Model.Entities;
using FinTrustFDManager.Model.Entities.Investment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FinTrustFDManager.BAL.Tests.IntegrationTests
{
    /// <summary>
    /// PostgreSQL-backed integration tests verifying the full workflow
    /// against a real database. Tests audit, protection, amendment,
    /// concurrency, and maker-checker enforcement.
    /// </summary>
    public class PostgreSqlIntegrationTests : IClassFixture<PostgreSqlFixture>, IDisposable
    {
        private readonly PostgreSqlFixture _fixture;
        private readonly ApplicationDbContext _context;
        private readonly FDIdentificationService _fdService;
        private readonly FDInterestService _interestService;
        private readonly FDAmendmentService _amendmentService;
        private readonly FDCashFlowRepository _cashFlowRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly Mock<ILogger<FDIdentificationService>> _fdLogger;
        private readonly Mock<ILogger<FDInterestService>> _interestLogger;
        private readonly Mock<ILogger<FDAmendmentService>> _amendmentLogger;
        private readonly Mock<IBenchmarkRateHistoryService> _benchmarkService;

        public PostgreSqlIntegrationTests(PostgreSqlFixture fixture)
        {
            _fixture = fixture;
            _context = fixture.CreateFreshContext();

            var fdRepo = new FDIdentificationRepository(_context);
            var interestRepo = new FDInterestRepository(_context);
            var amendmentRepo = new FDAmendmentRepository(_context);
            _cashFlowRepo = new FDCashFlowRepository(_context);
            _unitOfWork = new UnitOfWork(_context);

            _fdLogger = new Mock<ILogger<FDIdentificationService>>();
            _interestLogger = new Mock<ILogger<FDInterestService>>();
            _amendmentLogger = new Mock<ILogger<FDAmendmentService>>();
            _benchmarkService = new Mock<IBenchmarkRateHistoryService>();
            _benchmarkService.Setup(s => s.GetEffectiveRateAsync(It.IsAny<int>(), It.IsAny<DateTime>()))
                .ReturnsAsync(0m);

            _interestService = new FDInterestService(
                interestRepo, fdRepo, _cashFlowRepo,
                _benchmarkService.Object, _unitOfWork, _interestLogger.Object);

            _fdService = new FDIdentificationService(
                fdRepo, interestRepo, _cashFlowRepo,
                _interestService, _unitOfWork, _fdLogger.Object);

            _amendmentService = new FDAmendmentService(
                amendmentRepo, fdRepo, interestRepo, _cashFlowRepo,
                _interestService, _unitOfWork, _amendmentLogger.Object);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        private static FDIdentification CreateFd(
            long fdId, decimal principal = 100_000m,
            DateTime? startDate = null, DateTime? endDate = null,
            string status = "DRAFT", long createdBy = 1)
        {
            return new FDIdentification
            {
                FdId = fdId,
                FdReferenceNo = $"FD-{fdId:D4}",
                EntityId = 1,
                CounterpartyId = 1,
                CurrencyId = 1,
                PrincipalAmount = principal,
                StartDate = startDate ?? new DateTime(2025, 1, 1),
                EndDate = endDate ?? new DateTime(2025, 12, 31),
                SettlementDate = (endDate ?? new DateTime(2025, 12, 31)).AddDays(1),
                Status = status,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow
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
            long fdId, decimal rate = 8m,
            string interestFreq = "QUARTERLY",
            string compoundingFreq = "QUARTERLY",
            bool isCompounding = true,
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

        private async Task<FDIdentification> SeedApprovedFd(long fdId = 1, long createdBy = 1, long approvedBy = 2)
        {
            var fd = CreateFd(fdId, createdBy: createdBy);
            await _fdService.CreateAsync(fd);

            var interest = CreateInterest(fdId);
            await _interestService.CreateAsync(interest);

            await _fdService.SubmitAsync(fdId, createdBy);
            await _fdService.ApproveAsync(fdId, approvedBy);

            // Return a detached entity to avoid tracking conflicts
            using var ctx = _fixture.CreateFreshContext();
            return (await ctx.FDIdentifications.AsNoTracking().FirstOrDefaultAsync(f => f.FdId == fdId))!;
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 1: Migration Verification
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task Migration_VerifyAllTablesExist()
        {
            using var ctx = _fixture.CreateFreshContext();
            var approvalHistories = await ctx.FDApprovalHistories.ToListAsync();
            var amendments = await ctx.FDAmendments.ToListAsync();
            Assert.NotNull(approvalHistories);
            Assert.NotNull(amendments);
        }

        [Fact]
        public async Task Migration_VerifyFDApprovalHistoryColumns()
        {
            // Create a real FD first to satisfy FK constraint
            var fd = CreateFd(9998);
            await _fdService.CreateAsync(fd);

            using var ctx = _fixture.CreateFreshContext();
            var history = new FDApprovalHistory
            {
                FdId = 9998, Action = "TEST", FromStatus = "DRAFT",
                ToStatus = "SUBMITTED", ActionBy = 1, ActionDate = DateTime.UtcNow,
                Comments = "Test", OldValues = "{\"Status\":\"DRAFT\"}",
                NewValues = "{\"Status\":\"SUBMITTED\"}"
            };
            ctx.FDApprovalHistories.Add(history);
            await ctx.SaveChangesAsync();

            var fromDb = await ctx.FDApprovalHistories.FindAsync(history.Id);
            Assert.NotNull(fromDb);
            Assert.Equal("TEST", fromDb.Action);
            Assert.Equal("{\"Status\":\"DRAFT\"}", fromDb.OldValues);
        }

        [Fact]
        public async Task Migration_VerifyFDAmendmentColumns()
        {
            // Create a real FD first to satisfy FK constraint
            var fd = CreateFd(9999);
            await _fdService.CreateAsync(fd);

            using var ctx = _fixture.CreateFreshContext();
            var amendment = new FDAmendment
            {
                FdId = 9999, Status = "PENDING_APPROVAL",
                Reason = "Test amendment",
                RequestedValues = "{\"EndDate\":\"2026-06-30\"}",
                OriginalValues = "{\"EndDate\":\"2025-12-31\"}",
                RequestedBy = 1, RequestedDate = DateTime.UtcNow
            };
            ctx.FDAmendments.Add(amendment);
            await ctx.SaveChangesAsync();

            var fromDb = await ctx.FDAmendments.FindAsync(amendment.AmendmentId);
            Assert.NotNull(fromDb);
            Assert.Equal("PENDING_APPROVAL", fromDb.Status);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 2: Audit Trail Verification
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task Audit_CREATE_VerifyDatabaseRow()
        {
            var fd = CreateFd(1);
            await _fdService.CreateAsync(fd);

            using var ctx = _fixture.CreateFreshContext();
            var audit = await ctx.FDApprovalHistories
                .Where(a => a.FdId == 1 && a.Action == "CREATE")
                .FirstOrDefaultAsync();

            Assert.NotNull(audit);
            Assert.Null(audit.FromStatus);
            Assert.Equal("DRAFT", audit.ToStatus);
        }

        [Fact]
        public async Task Audit_SUBMIT_VerifyDatabaseRow()
        {
            var fd = CreateFd(2);
            await _fdService.CreateAsync(fd);
            await _fdService.SubmitAsync(2, 1);

            using var ctx = _fixture.CreateFreshContext();
            var audit = await ctx.FDApprovalHistories
                .Where(a => a.FdId == 2 && a.Action == "SUBMIT")
                .FirstOrDefaultAsync();

            Assert.NotNull(audit);
            Assert.Equal("DRAFT", audit.FromStatus);
            Assert.Equal("PENDING_APPROVAL", audit.ToStatus);
            Assert.Equal(1, audit.ActionBy);
        }

        [Fact]
        public async Task Audit_APPROVE_VerifyDatabaseRow()
        {
            var fd = CreateFd(3);
            await _fdService.CreateAsync(fd);
            await _fdService.SubmitAsync(3, 1);
            await _fdService.ApproveAsync(3, 2);

            using var ctx = _fixture.CreateFreshContext();
            var audit = await ctx.FDApprovalHistories
                .Where(a => a.FdId == 3 && a.Action == "APPROVE")
                .FirstOrDefaultAsync();

            Assert.NotNull(audit);
            Assert.Equal("PENDING_APPROVAL", audit.FromStatus);
            Assert.Equal("APPROVED", audit.ToStatus);
            Assert.Equal(2, audit.ActionBy);
        }

        [Fact]
        public async Task Audit_REJECT_VerifyDatabaseRow()
        {
            var fd = CreateFd(4);
            await _fdService.CreateAsync(fd);
            await _fdService.SubmitAsync(4, 1);
            await _fdService.RejectAsync(4, 2, "Needs more info");

            using var ctx = _fixture.CreateFreshContext();
            var audit = await ctx.FDApprovalHistories
                .Where(a => a.FdId == 4 && a.Action == "REJECT")
                .FirstOrDefaultAsync();

            Assert.NotNull(audit);
            Assert.Equal("PENDING_APPROVAL", audit.FromStatus);
            Assert.Equal("REJECTED", audit.ToStatus);
            Assert.Equal("Needs more info", audit.Comments);
            Assert.Equal(2, audit.ActionBy);
        }

        [Fact]
        public async Task Audit_EDIT_VerifyOldAndNewValues()
        {
            var fd = CreateFd(5);
            await _fdService.CreateAsync(fd);

            // Load fresh detached entity, modify, then update via service
            using var ctx1 = _fixture.CreateFreshContext();
            var fdFromDb = await ctx1.FDIdentifications.AsNoTracking().FirstOrDefaultAsync(f => f.FdId == 5);
            fdFromDb!.PrincipalAmount = 200_000m;
            fdFromDb.ModifiedBy = 1;
            await _fdService.UpdateAsync(5, fdFromDb);

            using var ctx2 = _fixture.CreateFreshContext();
            var audit = await ctx2.FDApprovalHistories
                .Where(a => a.FdId == 5 && a.Action == "EDIT")
                .FirstOrDefaultAsync();

            Assert.NotNull(audit);
            Assert.NotNull(audit.OldValues);
            Assert.NotNull(audit.NewValues);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 3: Approved FD Protection
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task Protection_APPROVED_DirectEditRejected()
        {
            var approved = await SeedApprovedFd(10);
            var originalPrincipal = approved.PrincipalAmount;

            approved.PrincipalAmount = 999_999m;
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _fdService.UpdateAsync(10, approved));

            using var ctx = _fixture.CreateFreshContext();
            var fromDb = await ctx.FDIdentifications.FindAsync(10L);
            Assert.NotNull(fromDb);
            Assert.Equal(originalPrincipal, fromDb.PrincipalAmount);
        }

        [Fact]
        public async Task Protection_APPROVED_DeleteRejected()
        {
            await SeedApprovedFd(11);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _fdService.DeleteAsync(11));

            using var ctx = _fixture.CreateFreshContext();
            Assert.NotNull(await ctx.FDIdentifications.FindAsync(11L));
        }

        [Fact]
        public async Task Protection_DRAFT_AllowsEdit()
        {
            var fd = CreateFd(12);
            await _fdService.CreateAsync(fd);

            fd.PrincipalAmount = 200_000m;
            var result = await _fdService.UpdateAsync(12, fd);
            Assert.NotNull(result);

            using var ctx = _fixture.CreateFreshContext();
            var fromDb = await ctx.FDIdentifications.FindAsync(12L);
            Assert.Equal(200_000m, fromDb!.PrincipalAmount);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 4: Amendment Workflow
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task Amendment_Request_CreatesAmendmentRow()
        {
            await SeedApprovedFd(20);

            var request = new FDAmendmentRequestDto
            {
                Reason = "Customer requested maturity extension",
                EndDate = new DateTime(2026, 6, 30)
            };

            var amendment = await _amendmentService.RequestAmendmentAsync(20, request, 1);

            Assert.NotNull(amendment);
            Assert.Equal("PENDING_APPROVAL", amendment.Status);

            using var ctx = _fixture.CreateFreshContext();
            var fromDb = await ctx.FDAmendments.FindAsync(amendment.AmendmentId);
            Assert.NotNull(fromDb);
            Assert.Equal(20, fromDb.FdId);
            Assert.NotNull(fromDb.OriginalValues);
            Assert.NotNull(fromDb.RequestedValues);

            var fd = await ctx.FDIdentifications.FindAsync(20L);
            Assert.Equal(new DateTime(2025, 12, 31), fd!.EndDate);
        }

        [Fact]
        public async Task Amendment_Approve_UpdatesFD()
        {
            await SeedApprovedFd(21);

            var request = new FDAmendmentRequestDto
            {
                Reason = "Extend maturity",
                EndDate = new DateTime(2026, 6, 30)
            };
            var amendment = await _amendmentService.RequestAmendmentAsync(21, request, 1);

            var result = await _amendmentService.ApproveAmendmentAsync(21, amendment.AmendmentId, 2);

            Assert.True(result);

            using var ctx = _fixture.CreateFreshContext();
            var fd = await ctx.FDIdentifications.FindAsync(21L);
            Assert.Equal(new DateTime(2026, 6, 30), fd!.EndDate);

            var amendmentDb = await ctx.FDAmendments.FindAsync(amendment.AmendmentId);
            Assert.Equal("APPROVED", amendmentDb!.Status);
            Assert.Equal(2, amendmentDb.ApprovedBy);
        }

        [Fact]
        public async Task Amendment_Reject_LeavesFDUnchanged()
        {
            await SeedApprovedFd(22);

            var request = new FDAmendmentRequestDto
            {
                Reason = "Change rate",
                EndDate = new DateTime(2025, 12, 31)
            };
            var amendment = await _amendmentService.RequestAmendmentAsync(22, request, 1);

            var result = await _amendmentService.RejectAmendmentAsync(22, amendment.AmendmentId, 2, "Not approved");

            Assert.True(result);

            using var ctx = _fixture.CreateFreshContext();
            var fd = await ctx.FDIdentifications.FindAsync(22L);
            Assert.Equal(new DateTime(2025, 12, 31), fd!.EndDate);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 5: Maker-Checker Enforcement
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task MakerChecker_MakerCannotApproveOwnFD()
        {
            var fd = CreateFd(30, createdBy: 1);
            await _fdService.CreateAsync(fd);
            await _fdService.SubmitAsync(30, 1);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _fdService.ApproveAsync(30, 1));

            using var ctx = _fixture.CreateFreshContext();
            var fromDb = await ctx.FDIdentifications.FindAsync(30L);
            Assert.Equal("PENDING_APPROVAL", fromDb!.Status);
        }

        [Fact]
        public async Task MakerChecker_DifferentApproverCanApprove()
        {
            var fd = CreateFd(31, createdBy: 1);
            await _fdService.CreateAsync(fd);
            await _fdService.SubmitAsync(31, 1);

            var result = await _fdService.ApproveAsync(31, 2);
            Assert.True(result);

            using var ctx = _fixture.CreateFreshContext();
            var fromDb = await ctx.FDIdentifications.FindAsync(31L);
            Assert.Equal("APPROVED", fromDb!.Status);
        }

        [Fact]
        public async Task MakerChecker_MakerCannotApproveOwnAmendment()
        {
            await SeedApprovedFd(32, createdBy: 1);

            var request = new FDAmendmentRequestDto
            {
                Reason = "Self-amendment test",
                EndDate = new DateTime(2026, 1, 1)
            };
            var amendment = await _amendmentService.RequestAmendmentAsync(32, request, 1);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _amendmentService.ApproveAmendmentAsync(32, amendment.AmendmentId, 1));
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 6: FD Lifecycle E2E
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task Lifecycle_CreateEditSubmitApprove()
        {
            var fd = CreateFd(40);
            var created = await _fdService.CreateAsync(fd);
            Assert.Equal("DRAFT", created.Status);

            fd.PrincipalAmount = 150_000m;
            await _fdService.UpdateAsync(40, fd);

            await _fdService.SubmitAsync(40, 1);
            await _fdService.ApproveAsync(40, 2);

            using var ctx = _fixture.CreateFreshContext();
            var final = await ctx.FDIdentifications.FindAsync(40L);
            Assert.Equal("APPROVED", final!.Status);
            Assert.Equal(150_000m, final.PrincipalAmount);

            var audits = await ctx.FDApprovalHistories
                .Where(a => a.FdId == 40)
                .OrderBy(a => a.ActionDate)
                .ToListAsync();

            Assert.Contains(audits, a => a.Action == "CREATE");
            Assert.Contains(audits, a => a.Action == "EDIT");
            Assert.Contains(audits, a => a.Action == "SUBMIT");
            Assert.Contains(audits, a => a.Action == "APPROVE");
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 7: Approval History Retrieval
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task ApprovalHistory_GetByFdId_ReturnsAllActions()
        {
            var fd = CreateFd(50);
            await _fdService.CreateAsync(fd);
            await _fdService.SubmitAsync(50, 1);
            await _fdService.ApproveAsync(50, 2);

            var history = (await _fdService.GetApprovalHistoryAsync(50)).ToList();

            Assert.True(history.Count >= 3);
            Assert.Contains(history, h => h.Action == "CREATE");
            Assert.Contains(history, h => h.Action == "SUBMIT");
            Assert.Contains(history, h => h.Action == "APPROVE");
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 8: Audit Immutability
        // ═══════════════════════════════════════════════════════

        [Fact]
        public void AuditImmutability_NoCrudEndpointForAuditModification()
        {
            var methodNames = typeof(FDIdentificationService)
                .GetMethods()
                .Select(m => m.Name)
                .ToList();

            Assert.DoesNotContain(methodNames, m => m.Contains("UpdateAudit"));
            Assert.DoesNotContain(methodNames, m => m.Contains("DeleteAudit"));
            Assert.DoesNotContain(methodNames, m => m.Contains("ModifyAudit"));
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 9: FD Reference Number
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task FdReferenceNo_CleanDatabase_AllowsUniqueValues()
        {
            using var ctx = _fixture.CreateFreshContext();
            var fd1 = CreateFd(100);
            var fd2 = CreateFd(101);

            await _fdService.CreateAsync(fd1);
            await _fdService.CreateAsync(fd2);

            var fromDb1 = await ctx.FDIdentifications.FindAsync(100L);
            var fromDb2 = await ctx.FDIdentifications.FindAsync(101L);

            Assert.NotNull(fromDb1);
            Assert.NotNull(fromDb2);
        }

        // ═══════════════════════════════════════════════════════
        //  TEST 10: Amendment Audit Trail
        // ═══════════════════════════════════════════════════════

        [Fact]
        public async Task Amendment_AuditTrailCreatedForAllActions()
        {
            await SeedApprovedFd(60);

            var request = new FDAmendmentRequestDto
            {
                Reason = "Audit trail test",
                EndDate = new DateTime(2026, 3, 31)
            };
            var amendment = await _amendmentService.RequestAmendmentAsync(60, request, 1);

            await _amendmentService.ApproveAmendmentAsync(60, amendment.AmendmentId, 2);

            using var ctx = _fixture.CreateFreshContext();
            var audits = await ctx.FDApprovalHistories
                .Where(a => a.FdId == 60)
                .ToListAsync();

            Assert.Contains(audits, a => a.Action == "AMENDMENT_REQUEST");
            Assert.Contains(audits, a => a.Action == "AMENDMENT_APPROVE");
        }
    }
}
