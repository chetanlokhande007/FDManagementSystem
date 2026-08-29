using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.BAL.Services;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.Entities;
using FinTrustFDManager.Model.Entities.Investment;
using FinTrustFDManager.Model.Enums;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FinTrustFDManager.BAL.Tests
{
    public class FDWorkflowTests
    {
        private readonly Mock<IFDIdentificationRepository> _fdRepo;
        private readonly Mock<IFDInterestRepository> _interestRepo;
        private readonly Mock<IFDCashFlowRepository> _cashFlowRepo;
        private readonly Mock<IFDInterestService> _interestService;
        private readonly Mock<IUnitOfWork> _unitOfWork;
        private readonly Mock<ILogger<FDIdentificationService>> _logger;
        private readonly FDIdentificationService _service;

        public FDWorkflowTests()
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
                CurrencyId = 1,
                PrincipalAmount = 100_000m,
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 12, 31),
                SettlementDate = new DateTime(2026, 1, 1),
                Status = status,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow
            };
        }

        // ================================================
        // STATE MACHINE TESTS
        // ================================================

        [Theory]
        [InlineData("DRAFT", "SUBMITTED")]
        [InlineData("SUBMITTED", "PENDING_APPROVAL")]
        [InlineData("PENDING_APPROVAL", "APPROVED")]
        [InlineData("PENDING_APPROVAL", "REJECTED")]
        [InlineData("APPROVED", "ACTIVE")]
        [InlineData("ACTIVE", "MATURED")]
        [InlineData("REJECTED", "SUBMITTED")]
        public void StateMachine_AllValidTransitions_Allowed(string from, string to)
        {
            var error = FDStatus.ValidateTransition(from, to);
            Assert.Null(error);
        }

        [Theory]
        [InlineData("DRAFT", "APPROVED")]
        [InlineData("DRAFT", "ACTIVE")]
        [InlineData("DRAFT", "MATURED")]
        [InlineData("DRAFT", "REJECTED")]
        [InlineData("APPROVED", "DRAFT")]
        [InlineData("APPROVED", "PENDING_APPROVAL")]
        [InlineData("PENDING_APPROVAL", "ACTIVE")]
        [InlineData("PENDING_APPROVAL", "MATURED")]
        [InlineData("ACTIVE", "DRAFT")]
        [InlineData("ACTIVE", "APPROVED")]
        [InlineData("MATURED", "DRAFT")]
        [InlineData("MATURED", "APPROVED")]
        [InlineData("REJECTED", "APPROVED")]
        [InlineData("REJECTED", "ACTIVE")]
        public void StateMachine_InvalidTransitions_Rejected(string from, string to)
        {
            var error = FDStatus.ValidateTransition(from, to);
            Assert.NotNull(error);
            Assert.Contains(from, error!);
            Assert.Contains(to, error!);
        }

        // ================================================
        // CREATE TESTS
        // ================================================

        [Fact]
        public async Task CreateAsync_SetsStatusToDRAFT_AndGeneratesReference()
        {
            var model = CreateDraftFd();
            model.FdId = 0;
            model.FdReferenceNo = string.Empty;

            var result = await _service.CreateAsync(model);

            Assert.Equal(FDStatus.Draft, result.Status);
            Assert.Equal("FD-0001", result.FdReferenceNo);
            _fdRepo.Verify(r => r.AddApprovalHistoryAsync(It.Is<FDApprovalHistory>(
                h => h.Action == FDAction.Create && h.ToStatus == FDStatus.Draft)), Times.Once);
        }

        // ================================================
        // UPDATE PROTECTION TESTS
        // ================================================

        [Theory]
        [InlineData("APPROVED")]
        [InlineData("ACTIVE")]
        [InlineData("MATURED")]
        public async Task UpdateAsync_ProtectedStatus_ThrowsInvalidOperation(string status)
        {
            var fd = CreateDraftFd(status: status);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateAsync(fd.FdId, fd));
        }

        [Theory]
        [InlineData("DRAFT")]
        [InlineData("REJECTED")]
        public async Task UpdateAsync_EditableStatus_Succeeds(string status)
        {
            var fd = CreateDraftFd(status: status);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            var result = await _service.UpdateAsync(fd.FdId, fd);

            Assert.NotNull(result);
            Assert.Equal(status, result!.Status); // Status preserved
        }

        // ================================================
        // DELETE PROTECTION TESTS
        // ================================================

        [Theory]
        [InlineData("APPROVED")]
        [InlineData("ACTIVE")]
        [InlineData("MATURED")]
        public async Task DeleteAsync_ProtectedStatus_ThrowsInvalidOperation(string status)
        {
            var fd = CreateDraftFd(status: status);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.DeleteAsync(fd.FdId));
        }

        [Theory]
        [InlineData("DRAFT")]
        [InlineData("REJECTED")]
        public async Task DeleteAsync_EditableStatus_Succeeds(string status)
        {
            var fd = CreateDraftFd(status: status);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);
            _fdRepo.Setup(r => r.DeleteAsync(fd.FdId)).ReturnsAsync(true);
            _cashFlowRepo.Setup(r => r.GetByFdIdAsync(fd.FdId)).ReturnsAsync(new List<FDCashFlow>());
            _interestRepo.Setup(r => r.GetByFdIdAsync(fd.FdId)).ReturnsAsync((FDInterest?)null);

            var result = await _service.DeleteAsync(fd.FdId);

            Assert.True(result);
        }

        // ================================================
        // SUBMIT TESTS
        // ================================================

        [Fact]
        public async Task SubmitAsync_DRAFT_SetsToPENDING_APPROVAL()
        {
            var fd = CreateDraftFd(status: "DRAFT");
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            var result = await _service.SubmitAsync(fd.FdId, 101);

            Assert.True(result);
            Assert.Equal(FDStatus.PendingFdAdmin, fd.Status);
            _fdRepo.Verify(r => r.AddApprovalHistoryAsync(It.Is<FDApprovalHistory>(
                h => h.Action == FDAction.Submit
                && h.FromStatus == FDStatus.Draft
                && h.ToStatus == FDStatus.PendingFdAdmin
                && h.ActionBy == 101)), Times.Once);
        }

        [Fact]
        public async Task SubmitAsync_REJECTED_SetsToPENDING_APPROVAL()
        {
            var fd = CreateDraftFd(status: "REJECTED");
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            var result = await _service.SubmitAsync(fd.FdId, 101);

            Assert.True(result);
            Assert.Equal(FDStatus.PendingFdAdmin, fd.Status);
        }

        [Theory]
        [InlineData("SUBMITTED")]
        [InlineData("PENDING_APPROVAL")]
        [InlineData("APPROVED")]
        [InlineData("ACTIVE")]
        [InlineData("MATURED")]
        public async Task SubmitAsync_InvalidStatus_ThrowsInvalidOperation(string status)
        {
            var fd = CreateDraftFd(status: status);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.SubmitAsync(fd.FdId, 101));
        }

        // ================================================
        // APPROVE TESTS
        // ================================================

        [Fact]
        public async Task ApproveAsync_PendingApproval_SetsToAPPROVED()
        {
            var fd = CreateDraftFd(status: "PENDING_APPROVAL", createdBy: 101);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            var result = await _service.ApproveAsync(fd.FdId, 205);

            Assert.True(result);
            Assert.Equal(FDStatus.Approved, fd.Status);
            _fdRepo.Verify(r => r.AddApprovalHistoryAsync(It.Is<FDApprovalHistory>(
                h => h.Action == FDAction.Approve
                && h.FromStatus == FDStatus.PendingFdAdmin
                && h.ToStatus == FDStatus.Approved
                && h.ActionBy == 205)), Times.Once);
        }

        [Fact]
        public async Task ApproveAsync_MakerCannotApproveOwnFD_ThrowsInvalidOperation()
        {
            // User 101 created the FD and tries to approve it
            var fd = CreateDraftFd(status: "PENDING_APPROVAL", createdBy: 101);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ApproveAsync(fd.FdId, 101));
        }

        [Theory]
        [InlineData("DRAFT")]
        [InlineData("SUBMITTED")]
        [InlineData("APPROVED")]
        [InlineData("ACTIVE")]
        [InlineData("MATURED")]
        [InlineData("REJECTED")]
        public async Task ApproveAsync_InvalidStatus_ThrowsInvalidOperation(string status)
        {
            var fd = CreateDraftFd(status: status, createdBy: 101);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ApproveAsync(fd.FdId, 205));
        }

        // ================================================
        // REJECT TESTS
        // ================================================

        [Fact]
        public async Task RejectAsync_PendingApproval_SetsToREJECTED()
        {
            var fd = CreateDraftFd(status: "PENDING_APPROVAL", createdBy: 101);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            var result = await _service.RejectAsync(fd.FdId, 205, "Incorrect maturity date");

            Assert.True(result);
            Assert.Equal(FDStatus.Rejected, fd.Status);
            _fdRepo.Verify(r => r.AddApprovalHistoryAsync(It.Is<FDApprovalHistory>(
                h => h.Action == FDAction.Reject
                && h.FromStatus == FDStatus.PendingFdAdmin
                && h.ToStatus == FDStatus.Rejected
                && h.ActionBy == 205
                && h.Comments == "Incorrect maturity date")), Times.Once);
        }

        [Fact]
        public async Task RejectAsync_MakerCannotRejectOwnFD_ThrowsInvalidOperation()
        {
            var fd = CreateDraftFd(status: "PENDING_APPROVAL", createdBy: 101);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.RejectAsync(fd.FdId, 101, "Some reason"));
        }

        [Fact]
        public async Task RejectAsync_NoComments_ThrowsInvalidOperation()
        {
            var fd = CreateDraftFd(status: "PENDING_APPROVAL", createdBy: 101);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.RejectAsync(fd.FdId, 205, ""));
        }

        [Fact]
        public async Task RejectAsync_ShortComments_ThrowsInvalidOperation()
        {
            var fd = CreateDraftFd(status: "PENDING_APPROVAL", createdBy: 101);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.RejectAsync(fd.FdId, 205, "Bad"));
        }

        // ================================================
        // FULL LIFECYCLE INTEGRATION TEST
        // ================================================

        [Fact]
        public async Task FullLifecycle_Create_Submit_Approve_Succeeds()
        {
            // 1. Create
            var model = CreateDraftFd();
            model.FdId = 0;
            model.FdReferenceNo = string.Empty;
            model.CreatedBy = 101;

            var created = await _service.CreateAsync(model);
            Assert.Equal(FDStatus.Draft, created.Status);

            // 2. Submit
            _fdRepo.Setup(r => r.GetByIdAsync(created.FdId)).ReturnsAsync(created);
            await _service.SubmitAsync(created.FdId, 101);
            Assert.Equal(FDStatus.PendingFdAdmin, created.Status);

            // 3. Approve (different user)
            await _service.ApproveAsync(created.FdId, 205);
            Assert.Equal(FDStatus.Approved, created.Status);
        }

        [Fact]
        public async Task FullLifecycle_Create_Submit_Reject_Resubmit_Approve_Succeeds()
        {
            // 1. Create
            var model = CreateDraftFd();
            model.FdId = 0;
            model.FdReferenceNo = string.Empty;
            model.CreatedBy = 101;
            var created = await _service.CreateAsync(model);

            // 2. Submit
            _fdRepo.Setup(r => r.GetByIdAsync(created.FdId)).ReturnsAsync(created);
            await _service.SubmitAsync(created.FdId, 101);
            Assert.Equal(FDStatus.PendingFdAdmin, created.Status);

            // 3. Reject
            await _service.RejectAsync(created.FdId, 205, "Incorrect maturity date, please fix");
            Assert.Equal(FDStatus.Rejected, created.Status);

            // 4. Resubmit
            await _service.SubmitAsync(created.FdId, 101);
            Assert.Equal(FDStatus.PendingFdAdmin, created.Status);

            // 5. Approve
            await _service.ApproveAsync(created.FdId, 205);
            Assert.Equal(FDStatus.Approved, created.Status);
        }

        [Fact]
        public async Task EditProtectedFD_AfterApproval_Throws()
        {
            var fd = CreateDraftFd(status: "APPROVED");
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateAsync(fd.FdId, fd));
        }

        [Fact]
        public async Task DeleteProtectedFD_AfterApproval_Throws()
        {
            var fd = CreateDraftFd(status: "APPROVED");
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.DeleteAsync(fd.FdId));
        }

        // ================================================
        // AUDIT TRAIL TESTS
        // ================================================

        [Fact]
        public async Task CreateAsync_CreatesAuditRecord()
        {
            var model = CreateDraftFd();
            model.FdId = 0;
            model.FdReferenceNo = string.Empty;

            await _service.CreateAsync(model);

            _fdRepo.Verify(r => r.AddApprovalHistoryAsync(It.Is<FDApprovalHistory>(
                h => h.Action == FDAction.Create
                && h.FdId == 1
                && h.ToStatus == FDStatus.Draft)), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_CreatesAuditRecord()
        {
            var fd = CreateDraftFd(status: "DRAFT");
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            await _service.UpdateAsync(fd.FdId, fd);

            _fdRepo.Verify(r => r.AddApprovalHistoryAsync(It.Is<FDApprovalHistory>(
                h => h.Action == FDAction.Edit
                && h.FromStatus == FDStatus.Draft
                && h.ToStatus == FDStatus.Draft)), Times.Once);
        }

        // ================================================
        // CONCURRENCY TEST (Double Approval)
        // ================================================

        [Fact]
        public async Task DoubleApproval_FirstSucceedsSecondFails()
        {
            // First approver: PENDING_APPROVAL → APPROVED (success)
            // Second approver: APPROVED → APPROVED (invalid transition)
            var fd = CreateDraftFd(status: "PENDING_APPROVAL", createdBy: 101);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            // First approval
            await _service.ApproveAsync(fd.FdId, 205);
            Assert.Equal(FDStatus.Approved, fd.Status);

            // Second approval should fail because status is now APPROVED
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ApproveAsync(fd.FdId, 300));
        }

        // ================================================
        // ADMIN ROLE TESTS
        // ================================================

        [Fact]
        public async Task Admin_CanCreateFD_SetsStatusToDRAFT()
        {
            // Admin UserId=1 creates an FD
            var model = CreateDraftFd(createdBy: 1);
            model.FdId = 0;
            model.FdReferenceNo = string.Empty;
            model.CreatedBy = 1;

            var result = await _service.CreateAsync(model);

            Assert.Equal(FDStatus.Draft, result.Status);
            Assert.Equal(1, result.CreatedBy);
            _fdRepo.Verify(r => r.AddApprovalHistoryAsync(It.Is<FDApprovalHistory>(
                h => h.Action == FDAction.Create && h.ActionBy == 1)), Times.Once);
        }

        [Fact]
        public async Task Admin_CanSubmitFD()
        {
            var fd = CreateDraftFd(createdBy: 1, status: "DRAFT");
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            var result = await _service.SubmitAsync(fd.FdId, 1); // Admin submits own FD

            Assert.True(result);
            Assert.Equal(FDStatus.PendingFdAdmin, fd.Status);
        }

        [Fact]
        public async Task Admin_CannotApproveOwnFD_MakerCheckerEnforced()
        {
            // Admin (UserId=1) created and submitted FD-0001
            // Admin (UserId=1) now tries to approve it → MUST FAIL
            var fd = CreateDraftFd(status: "PENDING_APPROVAL", createdBy: 1);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ApproveAsync(fd.FdId, 1));

            Assert.Contains("Maker-Check violation", ex.Message);
        }

        [Fact]
        public async Task Admin_CannotRejectOwnFD_MakerCheckerEnforced()
        {
            var fd = CreateDraftFd(status: "PENDING_APPROVAL", createdBy: 1);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.RejectAsync(fd.FdId, 1, "Trying to reject my own FD"));

            Assert.Contains("Maker-Check violation", ex.Message);
        }

        [Fact]
        public async Task Admin_CanApproveOthersFD_IfDifferentUser()
        {
            // Admin UserId=2 approves FD created by UserId=101
            var fd = CreateDraftFd(status: "PENDING_APPROVAL", createdBy: 101);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            var result = await _service.ApproveAsync(fd.FdId, 2);

            Assert.True(result);
            Assert.Equal(FDStatus.Approved, fd.Status);
        }

        [Fact]
        public async Task Admin_CannotEditApprovedFD_ProtectionEnforced()
        {
            // Admin tries to edit an APPROVED FD → MUST FAIL
            var fd = CreateDraftFd(status: "APPROVED", createdBy: 1);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateAsync(fd.FdId, fd));
        }

        [Fact]
        public async Task Admin_CannotDeleteApprovedFD_ProtectionEnforced()
        {
            var fd = CreateDraftFd(status: "APPROVED", createdBy: 1);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.DeleteAsync(fd.FdId));
        }

        [Fact]
        public async Task Admin_CanEditDRAFTFD()
        {
            var fd = CreateDraftFd(status: "DRAFT", createdBy: 1);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            var result = await _service.UpdateAsync(fd.FdId, fd);

            Assert.NotNull(result);
            _fdRepo.Verify(r => r.AddApprovalHistoryAsync(It.Is<FDApprovalHistory>(
                h => h.Action == FDAction.Edit && h.ActionBy == 0)), Times.Once);
        }

        [Fact]
        public async Task Admin_CanDeleteDRAFTFD()
        {
            var fd = CreateDraftFd(status: "DRAFT", createdBy: 1);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);
            _fdRepo.Setup(r => r.DeleteAsync(fd.FdId)).ReturnsAsync(true);
            _cashFlowRepo.Setup(r => r.GetByFdIdAsync(fd.FdId)).ReturnsAsync(new List<FDCashFlow>());
            _interestRepo.Setup(r => r.GetByFdIdAsync(fd.FdId)).ReturnsAsync((FDInterest?)null);

            var result = await _service.DeleteAsync(fd.FdId);

            Assert.True(result);
        }

        [Fact]
        public async Task Admin_CreateEditSubmitApproveAllCreateAuditRecords()
        {
            // 1. Create
            var model = CreateDraftFd(createdBy: 1);
            model.FdId = 0;
            model.FdReferenceNo = string.Empty;
            model.CreatedBy = 1;
            var created = await _service.CreateAsync(model);

            _fdRepo.Verify(r => r.AddApprovalHistoryAsync(It.Is<FDApprovalHistory>(
                h => h.Action == FDAction.Create && h.ActionBy == 1)), Times.Once);

            // 2. Edit
            _fdRepo.Setup(r => r.GetByIdAsync(created.FdId)).ReturnsAsync(created);
            await _service.UpdateAsync(created.FdId, created);

            _fdRepo.Verify(r => r.AddApprovalHistoryAsync(It.Is<FDApprovalHistory>(
                h => h.Action == FDAction.Edit)), Times.Once);

            // 3. Submit
            await _service.SubmitAsync(created.FdId, 1);

            _fdRepo.Verify(r => r.AddApprovalHistoryAsync(It.Is<FDApprovalHistory>(
                h => h.Action == FDAction.Submit && h.ActionBy == 1)), Times.Once);

            // 4. Approve (by different user)
            await _service.ApproveAsync(created.FdId, 2);

            _fdRepo.Verify(r => r.AddApprovalHistoryAsync(It.Is<FDApprovalHistory>(
                h => h.Action == FDAction.Approve && h.ActionBy == 2)), Times.Once);
        }

        [Fact]
        public async Task TwoAdminUsers_SameUserCannotBeBothMakerAndApprover()
        {
            // Admin UserId=1 creates FD
            var model = CreateDraftFd(createdBy: 1);
            model.FdId = 0;
            model.FdReferenceNo = string.Empty;
            model.CreatedBy = 1;
            var created = await _service.CreateAsync(model);

            // Admin UserId=1 submits → PENDING_APPROVAL
            _fdRepo.Setup(r => r.GetByIdAsync(created.FdId)).ReturnsAsync(created);
            await _service.SubmitAsync(created.FdId, 1);
            Assert.Equal(FDStatus.PendingFdAdmin, created.Status);

            // Admin UserId=1 tries to approve own FD → FAIL (maker-checker)
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ApproveAsync(created.FdId, 1));
            Assert.Equal(FDStatus.PendingFdAdmin, created.Status); // status unchanged

            // Admin UserId=2 approves → SUCCESS
            var result = await _service.ApproveAsync(created.FdId, 2);
            Assert.True(result);
            Assert.Equal(FDStatus.Approved, created.Status);
        }

        // ================================================
        // MAKER ROLE TESTS
        // ================================================

        [Fact]
        public async Task Maker_CanCreateAndSubmitButCannotApprove()
        {
            // CA/Maker (UserId=101) creates FD
            var model = CreateDraftFd(createdBy: 101);
            model.FdId = 0;
            model.FdReferenceNo = string.Empty;
            model.CreatedBy = 101;
            var created = await _service.CreateAsync(model);

            // Maker submits
            _fdRepo.Setup(r => r.GetByIdAsync(created.FdId)).ReturnsAsync(created);
            await _service.SubmitAsync(created.FdId, 101);
            Assert.Equal(FDStatus.PendingFdAdmin, created.Status);

            // Maker tries to approve own FD → FAIL
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ApproveAsync(created.FdId, 101));
        }

        // ================================================
        // APPROVER ROLE TESTS
        // ================================================

        [Fact]
        public async Task Approver_CanApprovePendingFD()
        {
            var fd = CreateDraftFd(status: "PENDING_APPROVAL", createdBy: 101);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            var result = await _service.ApproveAsync(fd.FdId, 205);

            Assert.True(result);
            Assert.Equal(FDStatus.Approved, fd.Status);
        }

        [Fact]
        public async Task Approver_CanRejectPendingFD()
        {
            var fd = CreateDraftFd(status: "PENDING_APPROVAL", createdBy: 101);
            _fdRepo.Setup(r => r.GetByIdAsync(fd.FdId)).ReturnsAsync(fd);

            var result = await _service.RejectAsync(fd.FdId, 205, "Missing required documentation");

            Assert.True(result);
            Assert.Equal(FDStatus.Rejected, fd.Status);
        }

        // ================================================
        // FD STATUS ENUM TESTS
        // ================================================

        [Fact]
        public void FDStatus_IsProtected_ReturnsTrueForApprovedActiveMatured()
        {
            Assert.True(FDStatus.IsProtected("APPROVED"));
            Assert.True(FDStatus.IsProtected("ACTIVE"));
            Assert.True(FDStatus.IsProtected("MATURED"));
        }

        [Fact]
        public void FDStatus_IsProtected_ReturnsFalseForDraftRejected()
        {
            Assert.False(FDStatus.IsProtected("DRAFT"));
            Assert.False(FDStatus.IsProtected("REJECTED"));
            Assert.False(FDStatus.IsProtected("PENDING_APPROVAL"));
        }

        [Fact]
        public void FDStatus_IsEditable_ReturnsTrueForDraftRejected()
        {
            Assert.True(FDStatus.IsEditable("DRAFT"));
            Assert.True(FDStatus.IsEditable("REJECTED"));
        }

        [Fact]
        public void FDStatus_IsEditable_ReturnsFalseForProtectedStatuses()
        {
            Assert.False(FDStatus.IsEditable("APPROVED"));
            Assert.False(FDStatus.IsEditable("ACTIVE"));
            Assert.False(FDStatus.IsEditable("MATURED"));
            Assert.False(FDStatus.IsEditable("PENDING_APPROVAL"));
        }

        [Fact]
        public void FDStatus_IsValid_AllStatusesValid()
        {
            Assert.True(FDStatus.IsValid("DRAFT"));
            Assert.True(FDStatus.IsValid("SUBMITTED"));
            Assert.True(FDStatus.IsValid("PENDING_APPROVAL"));
            Assert.True(FDStatus.IsValid("APPROVED"));
            Assert.True(FDStatus.IsValid("ACTIVE"));
            Assert.True(FDStatus.IsValid("MATURED"));
            Assert.True(FDStatus.IsValid("REJECTED"));
            Assert.False(FDStatus.IsValid("INVALID"));
        }
    }
}
