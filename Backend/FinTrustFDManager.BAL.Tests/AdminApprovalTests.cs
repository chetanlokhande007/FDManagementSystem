using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.BAL.Services;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.DTOs.Investment;
using FinTrustFDManager.Model.Entities;
using FinTrustFDManager.Model.Entities.Investment;
using FinTrustFDManager.Model.Enums;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FinTrustFDManager.BAL.Tests
{
    public class AdminApprovalTests
    {
        private readonly Mock<IFDIdentificationRepository> _fdRepo;
        private readonly Mock<IFDInterestRepository> _interestRepo;
        private readonly Mock<IFDCashFlowRepository> _cashFlowRepo;
        private readonly Mock<IFDInterestService> _interestService;
        private readonly Mock<IUnitOfWork> _unitOfWork;
        private readonly Mock<ILogger<FDIdentificationService>> _logger;
        private readonly FDIdentificationService _service;

        public AdminApprovalTests()
        {
            _fdRepo = new Mock<IFDIdentificationRepository>();
            _interestRepo = new Mock<IFDInterestRepository>();
            _cashFlowRepo = new Mock<IFDCashFlowRepository>();
            _interestService = new Mock<IFDInterestService>();
            _unitOfWork = new Mock<IUnitOfWork>();
            _logger = new Mock<ILogger<FDIdentificationService>>();

            var mockTransaction = new Mock<IDbContextTransaction>();
            _unitOfWork.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(mockTransaction.Object);
            _unitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            _fdRepo.Setup(r => r.GetNextFdReferenceNoAsync()).ReturnsAsync("FD-0001");
            _fdRepo.Setup(r => r.AddAsync(It.IsAny<FDIdentification>()))
                .ReturnsAsync((FDIdentification m) => { m.FdId = 1; return m; });
            _fdRepo.Setup(r => r.UpdateAsync(It.IsAny<FDIdentification>()))
                .ReturnsAsync((FDIdentification m) => m);
            _fdRepo.Setup(r => r.AddApprovalHistoryAsync(It.IsAny<FDApprovalHistory>()))
                .Returns(Task.CompletedTask);
            _interestService.Setup(s => s.RegenerateCashFlowsAsync(It.IsAny<long>()))
                .ReturnsAsync(true);

            _service = new FDIdentificationService(
                _fdRepo.Object, _interestRepo.Object, _cashFlowRepo.Object,
                _interestService.Object, _unitOfWork.Object, _logger.Object);
        }

        private static FDIdentification CreateDraftFd(long fdId = 1, long createdBy = 101, string status = "DRAFT")
        {
            return new FDIdentification
            {
                FdId = fdId,
                FdReferenceNo = "FD-0001",
                EntityId = 1,
                CounterpartyId = 1,
                CurrencyCode = "INR",
                PrincipalAmount = 100_000m,
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 12, 31),
                SettlementDate = new DateTime(2026, 1, 1),
                Status = status,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow
            };
        }

        // ================================================================
        // GetAdminDashboardSummaryAsync TESTS
        // ================================================================

        [Fact]
        public async Task GetAdminDashboardSummaryAsync_ReturnsCorrectStatusCounts()
        {
            var statusCounts = new Dictionary<string, int>
            {
                { "PENDING_APPROVAL", 5 },
                { "APPROVED", 12 },
                { "REJECTED", 3 },
                { "DRAFT", 8 },
                { "ACTIVE", 15 }
            };
            _fdRepo.Setup(r => r.GetStatusCountsAsync()).ReturnsAsync(statusCounts);
            _fdRepo.Setup(r => r.GetCriticalPendingCountAsync(It.IsAny<decimal>())).ReturnsAsync(2);
            _fdRepo.Setup(r => r.GetRejectedTodayCountAsync()).ReturnsAsync(1);

            var result = await _service.GetAdminDashboardSummaryAsync();

            Assert.Equal(5, result.TotalPending);
            Assert.Equal(12, result.TotalApproved);
            Assert.Equal(3, result.TotalRejected);
            Assert.Equal(8, result.TotalDraft);
            Assert.Equal(15, result.TotalActive);
            Assert.Equal(2, result.CriticalPending);
            Assert.Equal(1, result.RejectedToday);
        }

        [Fact]
        public async Task GetAdminDashboardSummaryAsync_EmptyDatabase_ReturnsZeros()
        {
            _fdRepo.Setup(r => r.GetStatusCountsAsync())
                .ReturnsAsync(new Dictionary<string, int>());
            _fdRepo.Setup(r => r.GetCriticalPendingCountAsync(It.IsAny<decimal>())).ReturnsAsync(0);
            _fdRepo.Setup(r => r.GetRejectedTodayCountAsync()).ReturnsAsync(0);

            var result = await _service.GetAdminDashboardSummaryAsync();

            Assert.Equal(0, result.TotalPending);
            Assert.Equal(0, result.TotalApproved);
            Assert.Equal(0, result.TotalRejected);
            Assert.Equal(0, result.TotalDraft);
            Assert.Equal(0, result.CriticalPending);
            Assert.Equal(0, result.RejectedToday);
        }

        [Fact]
        public async Task GetAdminDashboardSummaryAsync_MissingStatusKey_ReturnsZero()
        {
            // Only PENDING_APPROVAL exists, other statuses are absent
            var statusCounts = new Dictionary<string, int>
            {
                { "PENDING_APPROVAL", 7 }
            };
            _fdRepo.Setup(r => r.GetStatusCountsAsync()).ReturnsAsync(statusCounts);
            _fdRepo.Setup(r => r.GetCriticalPendingCountAsync(It.IsAny<decimal>())).ReturnsAsync(0);
            _fdRepo.Setup(r => r.GetRejectedTodayCountAsync()).ReturnsAsync(0);

            var result = await _service.GetAdminDashboardSummaryAsync();

            Assert.Equal(7, result.TotalPending);
            Assert.Equal(0, result.TotalApproved);  // Missing key → 0
            Assert.Equal(0, result.TotalRejected);  // Missing key → 0
            Assert.Equal(0, result.TotalDraft);     // Missing key → 0
        }

        // ================================================================
        // GetAdminApprovalListAsync TESTS
        // ================================================================

        [Fact]
        public async Task GetAdminApprovalListAsync_NoFilter_ReturnsAllRecords()
        {
            var fdList = new List<FDLandingDto>
            {
                CreateLandingDto(1, "FD-0001", "PENDING_APPROVAL"),
                CreateLandingDto(2, "FD-0002", "APPROVED"),
                CreateLandingDto(3, "FD-0003", "REJECTED")
            };
            _fdRepo.Setup(r => r.GetAdminApprovalListAsync(null)).ReturnsAsync(fdList);

            var result = (await _service.GetAdminApprovalListAsync(null)).ToList();

            Assert.Equal(3, result.Count);
        }

        [Fact]
        public async Task GetAdminApprovalListAsync_PendingFilter_ReturnsOnlyPending()
        {
            var fdList = new List<FDLandingDto>
            {
                CreateLandingDto(1, "FD-0001", "PENDING_APPROVAL"),
                CreateLandingDto(2, "FD-0002", "PENDING_APPROVAL")
            };
            _fdRepo.Setup(r => r.GetAdminApprovalListAsync("PENDING_APPROVAL")).ReturnsAsync(fdList);

            var result = (await _service.GetAdminApprovalListAsync("PENDING_APPROVAL")).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, fd => Assert.Equal("PENDING_APPROVAL", fd.Status));
        }

        [Fact]
        public async Task GetAdminApprovalListAsync_EmptyResult_ReturnsEmptyList()
        {
            _fdRepo.Setup(r => r.GetAdminApprovalListAsync("APPROVED"))
                .ReturnsAsync(new List<FDLandingDto>());

            var result = (await _service.GetAdminApprovalListAsync("APPROVED")).ToList();

            Assert.Empty(result);
        }

        // ================================================================
        // GetAdminApprovalDetailAsync TESTS
        // ================================================================

        [Fact]
        public async Task GetAdminApprovalDetailAsync_ExistingFd_ReturnsFullDetail()
        {
            var fd = CreateDraftFd(status: "PENDING_APPROVAL", createdBy: 101);
            fd.ModifiedBy = 205;

            var interest = new FDInterest
            {
                FdInterestId = 1,
                FdId = fd.FdId,
                InterestRateType = "FIXED",
                InterestRate = 7.5m,
                InterestFrequency = "Quarterly",
                IsCompounding = false,
                CalculationBasis = "ACTUAL_365",
                CreatedDate = DateTime.UtcNow
            };

            var cashFlows = new List<FDCashFlow>
            {
                new FDCashFlow { CashFlowId = 1, FdId = fd.FdId, Event = "FD Created",
                    StartDate = fd.StartDate, EndDate = fd.StartDate, Days = 0,
                    InterestRate = 7.5m, OpeningBalance = 0, InterestAmount = 0,
                    ClosingBalance = 100_000m, CashFlowAmount = 100_000m,
                    Direction = "OUTFLOW", CurrencyCode = "INR", Status = "PENDING",
                    ReferenceNo = "FD-0001", CreatedDate = DateTime.UtcNow },
                new FDCashFlow { CashFlowId = 2, FdId = fd.FdId, Event = "Maturity",
                    StartDate = fd.EndDate, EndDate = fd.EndDate, Days = 0,
                    InterestRate = 7.5m, OpeningBalance = 100_000m, InterestAmount = 0,
                    ClosingBalance = 0, CashFlowAmount = 107_500m,
                    Direction = "INFLOW", CurrencyCode = "INR", Status = "PENDING",
                    ReferenceNo = "FD-0001", CreatedDate = DateTime.UtcNow }
            };

            var approvalHistory = new List<FDApprovalHistory>
            {
                new FDApprovalHistory
                {
                    Id = 1, FdId = fd.FdId, Action = "CREATE",
                    FromStatus = null, ToStatus = "DRAFT",
                    ActionBy = 101, ActionDate = DateTime.UtcNow,
                    Comments = "FD created"
                }
            };

            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);
            _interestRepo.Setup(r => r.GetByFdIdAsync(fd.FdId)).ReturnsAsync(interest);
            _cashFlowRepo.Setup(r => r.GetByFdIdAsync(fd.FdId)).ReturnsAsync(cashFlows);
            _fdRepo.Setup(r => r.GetApprovalHistoryAsync(fd.FdId)).ReturnsAsync(approvalHistory);
            _fdRepo.Setup(r => r.GetUserNameAsync(101)).ReturnsAsync("John Maker");
            _fdRepo.Setup(r => r.GetUserNameAsync(205)).ReturnsAsync("Jane Approver");

            var result = await _service.GetAdminApprovalDetailAsync(fd.FdId);

            Assert.NotNull(result);
            Assert.Equal(fd.FdId, result!.FdId);
            Assert.Equal("FD-0001", result.FdReferenceNo);
            Assert.Equal("PENDING_APPROVAL", result.Status);
            Assert.Equal(100_000m, result.PrincipalAmount);
            Assert.Equal("John Maker", result.CreatedByName);
            Assert.Equal("Jane Approver", result.ModifiedByName);

            // Interest
            Assert.NotNull(result.Interest);
            Assert.Equal("FIXED", result.Interest!.InterestRateType);
            Assert.Equal(7.5m, result.Interest.InterestRate);
            Assert.Equal("Quarterly", result.Interest.InterestFrequency);
            Assert.False(result.Interest.IsCompounding);
            Assert.Equal("ACTUAL_365", result.Interest.CalculationBasis);

            // Cash flows
            Assert.Equal(2, result.CashFlows.Count);
            Assert.Equal("FD Created", result.CashFlows[0].Event);
            Assert.Equal("Maturity", result.CashFlows[1].Event);

            // Approval history
            Assert.Single(result.ApprovalHistory);
            Assert.Equal("CREATE", result.ApprovalHistory[0].Action);
            Assert.Equal("John Maker", result.ApprovalHistory[0].ActionByName);

            // Summary
            Assert.Equal(100_000m, result.TotalPrincipal);
            Assert.Equal(107_500m, result.MaturityAmount);
            Assert.True(result.TotalTenorDays > 0);
        }

        [Fact]
        public async Task GetAdminApprovalDetailAsync_NonExistentFd_ReturnsNull()
        {
            _fdRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((FDIdentification?)null);

            var result = await _service.GetAdminApprovalDetailAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAdminApprovalDetailAsync_NoInterestConfig_ReturnsNullInterest()
        {
            var fd = CreateDraftFd(status: "DRAFT");
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);
            _interestRepo.Setup(r => r.GetByFdIdAsync(fd.FdId)).ReturnsAsync((FDInterest?)null);
            _cashFlowRepo.Setup(r => r.GetByFdIdAsync(fd.FdId)).ReturnsAsync(new List<FDCashFlow>());
            _fdRepo.Setup(r => r.GetApprovalHistoryAsync(fd.FdId)).ReturnsAsync(new List<FDApprovalHistory>());
            _fdRepo.Setup(r => r.GetUserNameAsync(It.IsAny<long>())).ReturnsAsync("Unknown User");

            var result = await _service.GetAdminApprovalDetailAsync(fd.FdId);

            Assert.NotNull(result);
            Assert.Null(result!.Interest);
            Assert.Empty(result.CashFlows);
            Assert.Empty(result.ApprovalHistory);
        }

        [Fact]
        public async Task GetAdminApprovalDetailAsync_CompoundingInterest_CalculatesCorrectly()
        {
            var fd = CreateDraftFd(status: "APPROVED", createdBy: 101);
            fd.PrincipalAmount = 1_000_000m;

            var interest = new FDInterest
            {
                FdInterestId = 1, FdId = fd.FdId,
                InterestRateType = "FIXED", InterestRate = 8m,
                InterestFrequency = "Quarterly", IsCompounding = true,
                CompoundingFrequency = "Quarterly",
                CalculationBasis = "ACTUAL_365",
                CreatedDate = DateTime.UtcNow
            };

            var cashFlows = new List<FDCashFlow>
            {
                new FDCashFlow { CashFlowId = 1, FdId = fd.FdId, Event = "FD Created",
                    StartDate = fd.StartDate, EndDate = fd.StartDate, Days = 0,
                    InterestRate = 8m, OpeningBalance = 0, InterestAmount = 0,
                    ClosingBalance = 1_000_000m, CashFlowAmount = 1_000_000m,
                    Direction = "OUTFLOW", CurrencyCode = "INR", Status = "PENDING",
                    ReferenceNo = "FD-0001", CreatedDate = DateTime.UtcNow },
                new FDCashFlow { CashFlowId = 2, FdId = fd.FdId, Event = "Compounding Interest",
                    StartDate = fd.StartDate, EndDate = fd.StartDate.AddMonths(3), Days = 91,
                    InterestRate = 8m, OpeningBalance = 1_000_000m, InterestAmount = 20_000m,
                    ClosingBalance = 1_020_000m, CashFlowAmount = 0,
                    Direction = "INFLOW", CurrencyCode = "INR", Status = "PENDING",
                    ReferenceNo = "FD-0001", CreatedDate = DateTime.UtcNow },
                new FDCashFlow { CashFlowId = 3, FdId = fd.FdId, Event = "Maturity",
                    StartDate = fd.EndDate, EndDate = fd.EndDate, Days = 0,
                    InterestRate = 8m, OpeningBalance = 1_080_000m, InterestAmount = 0,
                    ClosingBalance = 0, CashFlowAmount = 1_080_000m,
                    Direction = "INFLOW", CurrencyCode = "INR", Status = "PENDING",
                    ReferenceNo = "FD-0001", CreatedDate = DateTime.UtcNow }
            };

            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);
            _interestRepo.Setup(r => r.GetByFdIdAsync(fd.FdId)).ReturnsAsync(interest);
            _cashFlowRepo.Setup(r => r.GetByFdIdAsync(fd.FdId)).ReturnsAsync(cashFlows);
            _fdRepo.Setup(r => r.GetApprovalHistoryAsync(fd.FdId)).ReturnsAsync(new List<FDApprovalHistory>());
            _fdRepo.Setup(r => r.GetUserNameAsync(It.IsAny<long>())).ReturnsAsync("User");

            var result = await _service.GetAdminApprovalDetailAsync(fd.FdId);

            Assert.NotNull(result);
            Assert.True(result!.Interest!.IsCompounding);
            Assert.Equal(1_000_000m, result.TotalPrincipal);
            // Compounding: totalInterest = maturityAmount - principal = 1,080,000 - 1,000,000
            Assert.Equal(80_000m, result.TotalInterest);
            Assert.Equal(1_080_000m, result.MaturityAmount);
        }

        [Fact]
        public async Task GetAdminApprovalDetailAsync_CreatesAuditHistoryInCorrectOrder()
        {
            var fd = CreateDraftFd(status: "APPROVED", createdBy: 101);
            var history = new List<FDApprovalHistory>
            {
                new FDApprovalHistory { Id = 1, FdId = fd.FdId, Action = "CREATE",
                    FromStatus = null, ToStatus = "DRAFT", ActionBy = 101,
                    ActionDate = new DateTime(2025, 1, 1, 10, 0, 0), Comments = "Created" },
                new FDApprovalHistory { Id = 2, FdId = fd.FdId, Action = "SUBMIT",
                    FromStatus = "DRAFT", ToStatus = "PENDING_APPROVAL", ActionBy = 101,
                    ActionDate = new DateTime(2025, 1, 2, 10, 0, 0), Comments = "Submitted" },
                new FDApprovalHistory { Id = 3, FdId = fd.FdId, Action = "APPROVE",
                    FromStatus = "PENDING_APPROVAL", ToStatus = "APPROVED", ActionBy = 205,
                    ActionDate = new DateTime(2025, 1, 3, 10, 0, 0), Comments = "Approved" }
            };

            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);
            _interestRepo.Setup(r => r.GetByFdIdAsync(fd.FdId)).ReturnsAsync((FDInterest?)null);
            _cashFlowRepo.Setup(r => r.GetByFdIdAsync(fd.FdId)).ReturnsAsync(new List<FDCashFlow>());
            _fdRepo.Setup(r => r.GetApprovalHistoryAsync(fd.FdId)).ReturnsAsync(history);
            _fdRepo.Setup(r => r.GetUserNameAsync(101)).ReturnsAsync("Maker");
            _fdRepo.Setup(r => r.GetUserNameAsync(205)).ReturnsAsync("Approver");

            var result = await _service.GetAdminApprovalDetailAsync(fd.FdId);

            Assert.NotNull(result);
            Assert.Equal(3, result!.ApprovalHistory.Count);
            Assert.Equal("CREATE", result.ApprovalHistory[0].Action);
            Assert.Equal("Maker", result.ApprovalHistory[0].ActionByName);
            Assert.Equal("SUBMIT", result.ApprovalHistory[1].Action);
            Assert.Equal("APPROVE", result.ApprovalHistory[2].Action);
            Assert.Equal("Approver", result.ApprovalHistory[2].ActionByName);
            Assert.Null(result.ApprovalHistory[0].FromStatus);  // CREATE has no previous status
            Assert.Equal("APPROVED", result.ApprovalHistory[2].ToStatus);
        }

        [Fact]
        public async Task GetAdminApprovalDetailAsync_UserNameResolution_ForCreatedByAndModifiedBy()
        {
            var fd = CreateDraftFd(status: "APPROVED", createdBy: 101);
            fd.ModifiedBy = 303;

            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);
            _interestRepo.Setup(r => r.GetByFdIdAsync(fd.FdId)).ReturnsAsync((FDInterest?)null);
            _cashFlowRepo.Setup(r => r.GetByFdIdAsync(fd.FdId)).ReturnsAsync(new List<FDCashFlow>());
            _fdRepo.Setup(r => r.GetApprovalHistoryAsync(fd.FdId)).ReturnsAsync(new List<FDApprovalHistory>());
            _fdRepo.Setup(r => r.GetUserNameAsync(101)).ReturnsAsync("Alice (Maker)");
            _fdRepo.Setup(r => r.GetUserNameAsync(303)).ReturnsAsync("Bob (Approver)");

            var result = await _service.GetAdminApprovalDetailAsync(fd.FdId);

            Assert.NotNull(result);
            Assert.Equal("Alice (Maker)", result!.CreatedByName);
            Assert.Equal("Bob (Approver)", result.ModifiedByName);
        }

        // ================================================================
        // Admin + Existing Workflow Integration Tests
        // ================================================================

        [Fact]
        public async Task AdminReviewFlow_ApproveThenDetailShowsApprovedStatus()
        {
            // Admin reviews a pending FD and approves it
            var fd = CreateDraftFd(status: "PENDING_APPROVAL", createdBy: 101);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            // Approve
            var approveResult = await _service.ApproveAsync(fd.FdId, 200);
            Assert.True(approveResult);
            Assert.Equal(FDStatus.Approved, fd.Status);

            // Now admin fetches detail - should show APPROVED
            var interest = new FDInterest
            {
                FdInterestId = 1, FdId = fd.FdId,
                InterestRateType = "FIXED", InterestRate = 7m,
                InterestFrequency = "Monthly", IsCompounding = false,
                CalculationBasis = "ACTUAL_365",
                CreatedDate = DateTime.UtcNow
            };

            _interestRepo.Setup(r => r.GetByFdIdAsync(fd.FdId)).ReturnsAsync(interest);
            _cashFlowRepo.Setup(r => r.GetByFdIdAsync(fd.FdId)).ReturnsAsync(new List<FDCashFlow>
            {
                new FDCashFlow { CashFlowId = 1, FdId = fd.FdId, Event = "FD Created",
                    StartDate = fd.StartDate, EndDate = fd.StartDate, Days = 0,
                    InterestRate = 7m, OpeningBalance = 0, InterestAmount = 0,
                    ClosingBalance = 100_000m, CashFlowAmount = 100_000m,
                    Direction = "OUTFLOW", CurrencyCode = "INR", Status = "PENDING",
                    ReferenceNo = "FD-0001", CreatedDate = DateTime.UtcNow }
            });

            var history = new List<FDApprovalHistory>
            {
                new FDApprovalHistory { Id = 1, FdId = fd.FdId, Action = "APPROVE",
                    FromStatus = "PENDING_APPROVAL", ToStatus = "APPROVED",
                    ActionBy = 200, ActionDate = DateTime.UtcNow, Comments = "Looks good" }
            };
            _fdRepo.Setup(r => r.GetApprovalHistoryAsync(fd.FdId)).ReturnsAsync(history);
            _fdRepo.Setup(r => r.GetUserNameAsync(101)).ReturnsAsync("Maker");
            _fdRepo.Setup(r => r.GetUserNameAsync(200)).ReturnsAsync("Admin Reviewer");

            var detail = await _service.GetAdminApprovalDetailAsync(fd.FdId);

            Assert.NotNull(detail);
            Assert.Equal("APPROVED", detail!.Status);
            Assert.Single(detail.ApprovalHistory);
            Assert.Equal("APPROVE", detail.ApprovalHistory[0].Action);
            Assert.Equal("Looks good", detail.ApprovalHistory[0].Comments);
        }

        [Fact]
        public async Task AdminReviewFlow_RejectThenDetailShowsRejectedStatus()
        {
            var fd = CreateDraftFd(status: "PENDING_APPROVAL", createdBy: 101);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            // Reject
            var rejectResult = await _service.RejectAsync(fd.FdId, 200, "Insufficient documentation provided");
            Assert.True(rejectResult);
            Assert.Equal(FDStatus.Rejected, fd.Status);

            // Detail should show REJECTED
            _interestRepo.Setup(r => r.GetByFdIdAsync(fd.FdId)).ReturnsAsync((FDInterest?)null);
            _cashFlowRepo.Setup(r => r.GetByFdIdAsync(fd.FdId)).ReturnsAsync(new List<FDCashFlow>());
            _fdRepo.Setup(r => r.GetApprovalHistoryAsync(fd.FdId)).ReturnsAsync(new List<FDApprovalHistory>
            {
                new FDApprovalHistory { Id = 1, FdId = fd.FdId, Action = "REJECT",
                    FromStatus = "PENDING_APPROVAL", ToStatus = "REJECTED",
                    ActionBy = 200, ActionDate = DateTime.UtcNow,
                    Comments = "Insufficient documentation provided" }
            });
            _fdRepo.Setup(r => r.GetUserNameAsync(It.IsAny<long>())).ReturnsAsync("User");

            var detail = await _service.GetAdminApprovalDetailAsync(fd.FdId);

            Assert.NotNull(detail);
            Assert.Equal("REJECTED", detail!.Status);
            Assert.Equal("Insufficient documentation provided", detail.ApprovalHistory[0].Comments);
        }

        [Fact]
        public async Task AdminReviewFlow_CannotApproveTwice_ConcurrencyProtection()
        {
            // Simulates two admin users opening the same pending FD
            var fd = CreateDraftFd(status: "PENDING_APPROVAL", createdBy: 101);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            // Admin A approves first
            await _service.ApproveAsync(fd.FdId, 200);
            Assert.Equal(FDStatus.Approved, fd.Status);

            // Admin B tries to approve - should fail (status is now APPROVED)
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ApproveAsync(fd.FdId, 300));
        }

        // ================================================================
        // Helper
        // ================================================================

        private static FDLandingDto CreateLandingDto(long fdId, string referenceNo, string status)
        {
            return new FDLandingDto
            {
                FdId = fdId,
                FdReferenceNo = referenceNo,
                EntityId = 1,
                EntityName = "Test Entity",
                CounterpartyId = 1,
                CounterPartyName = "Test Counterparty",
                CurrencyCode = "INR",
                PrincipalAmount = 100_000m,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddYears(1),
                Status = status,
                InterestRate = 7m,
                InterestRateType = "FIXED",
                InterestFrequency = "Monthly",
                CompoundingFrequency = "Not Applicable",
                CalculationBasis = "ACTUAL_365",
                TotalPrincipal = 100_000m,
                TotalGrossInterest = 7_000m,
                TotalTds = 0,
                TotalNetInterest = 7_000m,
                TotalAmount = 107_000m
            };
        }
    }
}
