using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.BAL.Services;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.DTOs.Amendment;
using FinTrustFDManager.Model.Entities;
using FinTrustFDManager.Model.Entities.Investment;
using FinTrustFDManager.Model.Enums;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FinTrustFDManager.BAL.Tests
{
    public class FDAmendmentTests
    {
        private readonly Mock<IFDAmendmentRepository> _amendmentRepo;
        private readonly Mock<IFDIdentificationRepository> _fdRepo;
        private readonly Mock<IFDInterestRepository> _interestRepo;
        private readonly Mock<IFDCashFlowRepository> _cashFlowRepo;
        private readonly Mock<IFDInterestService> _interestService;
        private readonly Mock<IUnitOfWork> _unitOfWork;
        private readonly Mock<ILogger<FDAmendmentService>> _logger;
        private readonly FDAmendmentService _service;

        public FDAmendmentTests()
        {
            _amendmentRepo = new Mock<IFDAmendmentRepository>();
            _fdRepo = new Mock<IFDIdentificationRepository>();
            _interestRepo = new Mock<IFDInterestRepository>();
            _cashFlowRepo = new Mock<IFDCashFlowRepository>();
            _interestService = new Mock<IFDInterestService>();
            _unitOfWork = new Mock<IUnitOfWork>();
            _logger = new Mock<ILogger<FDAmendmentService>>();

            var mockTransaction = new Mock<IDbContextTransaction>();
            _unitOfWork.Setup(u => u.BeginTransactionAsync()).ReturnsAsync(mockTransaction.Object);
            _unitOfWork.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
            _unitOfWork.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

            _fdRepo.Setup(r => r.AddApprovalHistoryAsync(It.IsAny<FDApprovalHistory>()))
                .Returns(Task.CompletedTask);

            _service = new FDAmendmentService(
                _amendmentRepo.Object, _fdRepo.Object, _interestRepo.Object,
                _cashFlowRepo.Object, _interestService.Object, _unitOfWork.Object, _logger.Object);
        }

        private static FDIdentification CreateApprovedFd(long fdId = 1, long createdBy = 101)
        {
            return new FDIdentification
            {
                FdId = fdId,
                FdReferenceNo = "FD-0001",
                EntityId = 1,
                CounterpartyId = 1,
                CurrencyId = 1,
                PrincipalAmount = 100_000m,
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 12, 31),
                SettlementDate = new DateTime(2026, 1, 1),
                Status = FDStatus.Approved,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow
            };
        }

        // ================================================
        // AMENDMENT REQUEST TESTS
        // ================================================

        [Fact]
        public async Task RequestAmendment_ApprovedFd_CreatesAmendment()
        {
            var fd = CreateApprovedFd();
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);
            _amendmentRepo.Setup(r => r.GetPendingByFdIdAsync(fd.FdId)).ReturnsAsync((FDAmendment?)null);
            _amendmentRepo.Setup(r => r.AddAsync(It.IsAny<FDAmendment>()))
                .ReturnsAsync((FDAmendment a) => { a.AmendmentId = 1; return a; });

            var request = new FDAmendmentRequestDto
            {
                Reason = "Customer requested maturity extension",
                EndDate = new DateTime(2026, 3, 31)
            };

            var result = await _service.RequestAmendmentAsync(fd.FdId, request, 101);

            Assert.Equal("PENDING_APPROVAL", result.Status);
            Assert.Equal(fd.FdId, result.FdId);
            Assert.Equal(101, result.RequestedBy);
            Assert.NotNull(result.OriginalValues);
            Assert.NotNull(result.RequestedValues);
        }

        [Fact]
        public async Task RequestAmendment_DraftFd_ThrowsInvalidOperation()
        {
            var fd = CreateApprovedFd();
            fd.Status = FDStatus.Draft;
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            var request = new FDAmendmentRequestDto { Reason = "Change principal", PrincipalAmount = 200_000m };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.RequestAmendmentAsync(fd.FdId, request, 101));
        }

        [Fact]
        public async Task RequestAmendment_PendingAmendmentExists_ThrowsInvalidOperation()
        {
            var fd = CreateApprovedFd();
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);
            _amendmentRepo.Setup(r => r.GetPendingByFdIdAsync(fd.FdId))
                .ReturnsAsync(new FDAmendment { AmendmentId = 99, Status = "PENDING_APPROVAL" });

            var request = new FDAmendmentRequestDto { Reason = "Change dates", EndDate = new DateTime(2026, 6, 30) };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.RequestAmendmentAsync(fd.FdId, request, 101));
        }

        [Fact]
        public async Task RequestAmendment_NoChanges_ThrowsInvalidOperation()
        {
            var fd = CreateApprovedFd();
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);
            _amendmentRepo.Setup(r => r.GetPendingByFdIdAsync(fd.FdId)).ReturnsAsync((FDAmendment?)null);

            var request = new FDAmendmentRequestDto { Reason = "No changes provided" };

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.RequestAmendmentAsync(fd.FdId, request, 101));
        }

        // ================================================
        // AMENDMENT APPROVAL TESTS
        // ================================================

        [Fact]
        public async Task ApproveAmendment_Pending_ApprovesAndApplies()
        {
            var fd = CreateApprovedFd();
            var amendment = new FDAmendment
            {
                AmendmentId = 1,
                FdId = fd.FdId,
                Status = "PENDING_APPROVAL",
                RequestedBy = 101,
                RequestedValues = "{\"endDate\":\"2026-03-31T00:00:00Z\"}",
                OriginalValues = "{\"endDate\":\"2025-12-31T00:00:00Z\"}",
                Reason = "Extend maturity"
            };

            _amendmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(amendment);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);
            _amendmentRepo.Setup(r => r.UpdateAsync(It.IsAny<FDAmendment>()))
                .ReturnsAsync((FDAmendment a) => a);
            _fdRepo.Setup(r => r.UpdateAsync(It.IsAny<FDIdentification>()))
                .ReturnsAsync((FDIdentification f) => f);
            _interestService.Setup(s => s.RegenerateCashFlowsAsync(fd.FdId)).ReturnsAsync(true);

            var result = await _service.ApproveAmendmentAsync(fd.FdId, 1, 205, "Approved");

            Assert.True(result);
            Assert.Equal("APPROVED", amendment.Status);
            Assert.Equal(205, amendment.ApprovedBy);
        }

        [Fact]
        public async Task ApproveAmendment_MakerCannotApproveOwn_ThrowsInvalidOperation()
        {
            var amendment = new FDAmendment
            {
                AmendmentId = 1,
                FdId = 1,
                Status = "PENDING_APPROVAL",
                RequestedBy = 101,
                RequestedValues = "{\"endDate\":\"2026-03-31T00:00:00Z\"}",
                OriginalValues = "{}"
            };

            _amendmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(amendment);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ApproveAmendmentAsync(1, 1, 101));
        }

        [Fact]
        public async Task ApproveAmendment_NotPending_ThrowsInvalidOperation()
        {
            var amendment = new FDAmendment
            {
                AmendmentId = 1,
                FdId = 1,
                Status = "APPROVED",
                RequestedBy = 101,
                RequestedValues = "{}",
                OriginalValues = "{}"
            };

            _amendmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(amendment);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ApproveAmendmentAsync(1, 1, 205));
        }

        // ================================================
        // AMENDMENT REJECTION TESTS
        // ================================================

        [Fact]
        public async Task RejectAmendment_Pending_RejectsSuccessfully()
        {
            var amendment = new FDAmendment
            {
                AmendmentId = 1,
                FdId = 1,
                Status = "PENDING_APPROVAL",
                RequestedBy = 101,
                RequestedValues = "{}",
                OriginalValues = "{}"
            };

            _amendmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(amendment);
            _amendmentRepo.Setup(r => r.UpdateAsync(It.IsAny<FDAmendment>()))
                .ReturnsAsync((FDAmendment a) => a);
            _fdRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(CreateApprovedFd());

            var result = await _service.RejectAmendmentAsync(1, 1, 205, "Not aligned with business requirements");

            Assert.True(result);
            Assert.Equal("REJECTED", amendment.Status);
            Assert.Equal(205, amendment.RejectedBy);
        }

        [Fact]
        public async Task RejectAmendment_MakerCannotRejectOwn_ThrowsInvalidOperation()
        {
            var amendment = new FDAmendment
            {
                AmendmentId = 1,
                FdId = 1,
                Status = "PENDING_APPROVAL",
                RequestedBy = 101,
                RequestedValues = "{}",
                OriginalValues = "{}"
            };

            _amendmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(amendment);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.RejectAmendmentAsync(1, 1, 101, "Rejecting my own amendment"));
        }

        [Fact]
        public async Task RejectAmendment_NoComments_ThrowsInvalidOperation()
        {
            var amendment = new FDAmendment
            {
                AmendmentId = 1,
                FdId = 1,
                Status = "PENDING_APPROVAL",
                RequestedBy = 101,
                RequestedValues = "{}",
                OriginalValues = "{}"
            };

            _amendmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(amendment);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.RejectAmendmentAsync(1, 1, 205, ""));
        }

        // ================================================
        // ORIGINAL FD UNCHANGED AFTER REJECTION
        // ================================================

        [Fact]
        public async Task RejectAmendment_OriginalFdRemainsUnchanged()
        {
            var fd = CreateApprovedFd();
            var originalEndDate = fd.EndDate;

            var amendment = new FDAmendment
            {
                AmendmentId = 1,
                FdId = fd.FdId,
                Status = "PENDING_APPROVAL",
                RequestedBy = 101,
                RequestedValues = "{\"endDate\":\"2026-06-30T00:00:00Z\"}",
                OriginalValues = $"{{\"endDate\":\"{fd.EndDate:O}\"}}"
            };

            _amendmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(amendment);
            _amendmentRepo.Setup(r => r.UpdateAsync(It.IsAny<FDAmendment>()))
                .ReturnsAsync((FDAmendment a) => a);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            await _service.RejectAmendmentAsync(fd.FdId, 1, 205, "Needs more documentation");

            // FD should NOT have been updated
            _fdRepo.Verify(r => r.UpdateAsync(It.IsAny<FDIdentification>()), Times.Never);
            _interestService.Verify(s => s.RegenerateCashFlowsAsync(It.IsAny<long>()), Times.Never);
            Assert.Equal(originalEndDate, fd.EndDate);
        }

        // ================================================
        // CONCURRENCY: DOUBLE APPROVAL
        // ================================================

        [Fact]
        public async Task DoubleApproval_FirstSucceedsSecondFails()
        {
            var fd = CreateApprovedFd();
            var amendment = new FDAmendment
            {
                AmendmentId = 1,
                FdId = fd.FdId,
                Status = "PENDING_APPROVAL",
                RequestedBy = 101,
                RequestedValues = "{\"endDate\":\"2026-03-31T00:00:00Z\"}",
                OriginalValues = "{}"
            };

            _amendmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(amendment);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);
            _amendmentRepo.Setup(r => r.UpdateAsync(It.IsAny<FDAmendment>()))
                .ReturnsAsync((FDAmendment a) => a);
            _fdRepo.Setup(r => r.UpdateAsync(It.IsAny<FDIdentification>()))
                .ReturnsAsync((FDIdentification f) => f);
            _interestService.Setup(s => s.RegenerateCashFlowsAsync(fd.FdId)).ReturnsAsync(true);

            // First approval succeeds
            await _service.ApproveAmendmentAsync(fd.FdId, 1, 205);
            Assert.Equal("APPROVED", amendment.Status);

            // Second approval fails (status is no longer PENDING_APPROVAL)
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ApproveAmendmentAsync(fd.FdId, 1, 300));
        }

        // ================================================
        // AUDIT TESTS
        // ================================================

        [Fact]
        public async Task ApproveAmendment_CreatesAuditRecord()
        {
            var fd = CreateApprovedFd();
            var amendment = new FDAmendment
            {
                AmendmentId = 1,
                FdId = fd.FdId,
                Status = "PENDING_APPROVAL",
                RequestedBy = 101,
                RequestedValues = "{\"endDate\":\"2026-03-31T00:00:00Z\"}",
                OriginalValues = "{}"
            };

            _amendmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(amendment);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);
            _amendmentRepo.Setup(r => r.UpdateAsync(It.IsAny<FDAmendment>()))
                .ReturnsAsync((FDAmendment a) => a);
            _fdRepo.Setup(r => r.UpdateAsync(It.IsAny<FDIdentification>()))
                .ReturnsAsync((FDIdentification f) => f);
            _interestService.Setup(s => s.RegenerateCashFlowsAsync(fd.FdId)).ReturnsAsync(true);

            await _service.ApproveAmendmentAsync(fd.FdId, 1, 205, "Approved");

            _fdRepo.Verify(r => r.AddApprovalHistoryAsync(It.Is<FDApprovalHistory>(
                h => h.Action == FDAction.AmendmentApprove
                && h.ActionBy == 205
                && h.OldValues != null
                && h.NewValues != null)), Times.Once);
        }

        [Fact]
        public async Task RejectAmendment_CreatesAuditRecord()
        {
            var fd = CreateApprovedFd();
            var amendment = new FDAmendment
            {
                AmendmentId = 1,
                FdId = fd.FdId,
                Status = "PENDING_APPROVAL",
                RequestedBy = 101,
                RequestedValues = "{}",
                OriginalValues = "{}"
            };

            _amendmentRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(amendment);
            _amendmentRepo.Setup(r => r.UpdateAsync(It.IsAny<FDAmendment>()))
                .ReturnsAsync((FDAmendment a) => a);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            await _service.RejectAmendmentAsync(fd.FdId, 1, 205, "Rejected for compliance reasons");

            _fdRepo.Verify(r => r.AddApprovalHistoryAsync(It.Is<FDApprovalHistory>(
                h => h.Action == FDAction.AmendmentReject
                && h.ActionBy == 205)), Times.Once);
        }
    }
}
