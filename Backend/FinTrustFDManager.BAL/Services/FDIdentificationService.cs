using FinTrustFDManager.BAL.Interfaces;
using FinTrustFDManager.DAL.Interfaces;
using FinTrustFDManager.Model.DTOs.Investment;
using FinTrustFDManager.Model.Entities;
using FinTrustFDManager.Model.Entities.Investment;
using FinTrustFDManager.Model.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinTrustFDManager.BAL.Services
{
    public class FDIdentificationService : IFDIdentificationService
    {
        private readonly IFDIdentificationRepository _repository;
        private readonly IFDInterestRepository _interestRepository;
        private readonly IFDCashFlowRepository _cashFlowRepository;
        private readonly IFDInterestService _interestService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<FDIdentificationService> _logger;

        public FDIdentificationService(
            IFDIdentificationRepository repository,
            IFDInterestRepository interestRepository,
            IFDCashFlowRepository cashFlowRepository,
            IFDInterestService interestService,
            IUnitOfWork unitOfWork,
            ILogger<FDIdentificationService> logger)
        {
            _repository = repository;
            _interestRepository = interestRepository;
            _cashFlowRepository = cashFlowRepository;
            _interestService = interestService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IEnumerable<FDIdentification>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<FDIdentification?> GetByIdAsync(long id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<FDIdentification> CreateAsync(FDIdentification model)
        {
            model.FdReferenceNo = await _repository.GetNextFdReferenceNoAsync();
            model.Status = FDStatus.Draft;
            model.CreatedDate = DateTime.UtcNow;
            model.StartDate = DateTime.SpecifyKind(model.StartDate, DateTimeKind.Utc);
            model.EndDate = DateTime.SpecifyKind(model.EndDate, DateTimeKind.Utc);
            if (model.SettlementDate.HasValue)
                model.SettlementDate = DateTime.SpecifyKind(model.SettlementDate.Value, DateTimeKind.Utc);

            var result = await _repository.AddAsync(model);

            await _repository.AddApprovalHistoryAsync(new FDApprovalHistory
            {
                FdId = result.FdId,
                Action = FDAction.Create,
                FromStatus = null,
                ToStatus = FDStatus.Draft,
                ActionBy = model.CreatedBy ?? 0,
                ActionDate = DateTime.UtcNow,
                Comments = "FD created"
            });

            return result;
        }

        public async Task<FDIdentification?> UpdateAsync(long id, FDIdentification model)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return null;

            if (FDStatus.IsProtected(existing.Status))
            {
                throw new InvalidOperationException(
                    $"Cannot edit FD '{existing.FdReferenceNo}' with status '{existing.Status}'. Only DRAFT or REJECTED FDs can be modified directly.");
            }

            model.FdId = id;
            model.StartDate = DateTime.SpecifyKind(model.StartDate, DateTimeKind.Utc);
            model.EndDate = DateTime.SpecifyKind(model.EndDate, DateTimeKind.Utc);
            if (model.SettlementDate.HasValue)
                model.SettlementDate = DateTime.SpecifyKind(model.SettlementDate.Value, DateTimeKind.Utc);
            model.ModifiedDate = DateTime.UtcNow;
            model.Status = existing.Status;

            var result = await _repository.UpdateAsync(model);

            if (result != null)
            {
                // Capture old/new values for financial audit trail
                var oldValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    existing.EntityId,
                    existing.CounterpartyId,
                    existing.CurrencyId,
                    existing.BankId,
                    existing.PrincipalAmount,
                    existing.StartDate,
                    existing.EndDate,
                    existing.SettlementDate
                });
                var newValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    result.EntityId,
                    result.CounterpartyId,
                    result.CurrencyId,
                    result.BankId,
                    result.PrincipalAmount,
                    result.StartDate,
                    result.EndDate,
                    result.SettlementDate
                });

                await _repository.AddApprovalHistoryAsync(new FDApprovalHistory
                {
                    FdId = result.FdId,
                    Action = FDAction.Edit,
                    FromStatus = existing.Status,
                    ToStatus = existing.Status,
                    ActionBy = model.ModifiedBy ?? 0,
                    ActionDate = DateTime.UtcNow,
                    Comments = "FD details updated",
                    OldValues = oldValues,
                    NewValues = newValues
                });
                await _interestService.RegenerateCashFlowsAsync(result.FdId);
            }

            return result;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return false;

            if (FDStatus.IsProtected(existing.Status))
            {
                throw new InvalidOperationException(
                    $"Cannot delete FD '{existing.FdReferenceNo}' with status '{existing.Status}'. Only DRAFT or REJECTED FDs can be deleted.");
            }

            var cashFlows = (await _cashFlowRepository.GetByFdIdAsync(id)).ToList();
            if (cashFlows.Count > 0) await _cashFlowRepository.DeleteRangeAsync(cashFlows);

            var interest = await _interestRepository.GetByFdIdAsync(id);
            if (interest != null) await _interestRepository.DeleteAsync(interest.FdInterestId);

            return await _repository.DeleteAsync(id);
        }

        public async Task<bool> SubmitAsync(long fdId, long userId)
        {
            var fd = await _repository.GetByIdAsync(fdId);
            if (fd == null) throw new KeyNotFoundException($"FD with ID {fdId} not found.");

            var error = FDStatus.ValidateTransition(fd.Status, FDStatus.PendingFdAdmin);
            if (error != null) throw new InvalidOperationException(error);

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                fd.Status = FDStatus.PendingFdAdmin;
                fd.ModifiedBy = userId;
                fd.ModifiedDate = DateTime.UtcNow;
                await _repository.UpdateAsync(fd);

                await _repository.AddApprovalHistoryAsync(new FDApprovalHistory
                {
                    FdId = fdId,
                    Action = FDAction.Submit,
                    FromStatus = FDStatus.Draft,
                    ToStatus = FDStatus.PendingFdAdmin,
                    ActionBy = userId,
                    ActionDate = DateTime.UtcNow,
                    Comments = "FD submitted for approval"
                });

                await _unitOfWork.CommitTransactionAsync();
                _logger.LogInformation("FD {Ref} (ID={FdId}) submitted by User {UserId}.", fd.FdReferenceNo, fdId, userId);
                return true;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> ApproveAsync(long fdId, long approverUserId, string? comments = null)
        {
            var fd = await _repository.GetByIdAsync(fdId);
            if (fd == null) throw new KeyNotFoundException($"FD with ID {fdId} not found.");

            var error = FDStatus.ValidateTransition(fd.Status, FDStatus.Approved);
            if (error != null) throw new InvalidOperationException(error);

            if (fd.CreatedBy.HasValue && fd.CreatedBy.Value == approverUserId)
                throw new InvalidOperationException($"Maker-Check violation: User {approverUserId} created this FD and cannot approve their own FD.");

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                fd.Status = FDStatus.Approved;
                fd.ModifiedBy = approverUserId;
                fd.ModifiedDate = DateTime.UtcNow;
                await _repository.UpdateAsync(fd);

                await _repository.AddApprovalHistoryAsync(new FDApprovalHistory
                {
                    FdId = fdId,
                    Action = FDAction.Approve,
                    FromStatus = FDStatus.PendingFdAdmin, // Temp fix, will be proper in full workflow implementation
                    ToStatus = FDStatus.Approved,
                    ActionBy = approverUserId,
                    ActionDate = DateTime.UtcNow,
                    Comments = comments ?? "Approved"
                });

                await _unitOfWork.CommitTransactionAsync();
                _logger.LogInformation("FD {Ref} (ID={FdId}) approved by User {UserId}.", fd.FdReferenceNo, fdId, approverUserId);
                return true;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> RejectAsync(long fdId, long approverUserId, string comments)
        {
            if (string.IsNullOrWhiteSpace(comments) || comments.Length < 5)
                throw new InvalidOperationException("Rejection reason is required (minimum 5 characters).");

            var fd = await _repository.GetByIdAsync(fdId);
            if (fd == null) throw new KeyNotFoundException($"FD with ID {fdId} not found.");

            var error = FDStatus.ValidateTransition(fd.Status, FDStatus.Rejected);
            if (error != null) throw new InvalidOperationException(error);

            if (fd.CreatedBy.HasValue && fd.CreatedBy.Value == approverUserId)
                throw new InvalidOperationException($"Maker-Check violation: User {approverUserId} created this FD and cannot reject their own FD.");

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                fd.Status = FDStatus.Rejected;
                fd.ModifiedBy = approverUserId;
                fd.ModifiedDate = DateTime.UtcNow;
                await _repository.UpdateAsync(fd);

                await _repository.AddApprovalHistoryAsync(new FDApprovalHistory
                {
                    FdId = fdId,
                    Action = FDAction.Reject,
                    FromStatus = FDStatus.PendingFdAdmin, // Temp fix
                    ToStatus = FDStatus.Rejected,
                    ActionBy = approverUserId,
                    ActionDate = DateTime.UtcNow,
                    Comments = comments
                });

                await _unitOfWork.CommitTransactionAsync();
                _logger.LogInformation("FD {Ref} (ID={FdId}) rejected by User {UserId}.", fd.FdReferenceNo, fdId, approverUserId);
                return true;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<IEnumerable<FDApprovalHistory>> GetApprovalHistoryAsync(long fdId)
        {
            return await _repository.GetApprovalHistoryAsync(fdId);
        }

        public async Task<IEnumerable<FDLandingDto>> GetLandingDataAsync()
        {
            return await _repository.GetLandingDataAsync();
        }
    }
}
