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
                    existing.CurrencyCode,
                    existing.PrincipalAmount,
                    existing.StartDate,
                    existing.EndDate,
                    existing.SettlementDate
                });
                var newValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    result.EntityId,
                    result.CounterpartyId,
                    result.CurrencyCode,
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

            var error = FDStatus.ValidateTransition(fd.Status, FDStatus.Submitted);
            if (error != null) throw new InvalidOperationException(error);

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                fd.Status = FDStatus.PendingApproval;
                fd.ModifiedBy = userId;
                fd.ModifiedDate = DateTime.UtcNow;
                await _repository.UpdateAsync(fd);

                await _repository.AddApprovalHistoryAsync(new FDApprovalHistory
                {
                    FdId = fdId,
                    Action = FDAction.Submit,
                    FromStatus = FDStatus.Draft,
                    ToStatus = FDStatus.PendingApproval,
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
                    FromStatus = FDStatus.PendingApproval,
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
                    FromStatus = FDStatus.PendingApproval,
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

        public async Task<IEnumerable<FDLandingDto>> GetPendingApprovalsAsync()
        {
            return await _repository.GetPendingApprovalsAsync();
        }

        public async Task<ApproverDashboardDto> GetApproverDashboardSummaryAsync(long approverUserId)
        {
            // Critical threshold: read from configuration, fallback to 10M (1 Crore)
            var criticalThreshold = 10_000_000m; // default

            var totalPending = await _repository.GetPendingCountAsync();
            var criticalPending = await _repository.GetCriticalPendingCountAsync(criticalThreshold);
            var approvedToday = await _repository.GetApprovedTodayCountAsync(approverUserId);

            return new ApproverDashboardDto
            {
                TotalPending = totalPending,
                CriticalPending = criticalPending,
                ApprovedToday = approvedToday
            };
        }

        public async Task<AdminDashboardSummaryDto> GetAdminDashboardSummaryAsync()
        {
            var statusCounts = await _repository.GetStatusCountsAsync();
            var criticalThreshold = 10_000_000m;
            var criticalPending = await _repository.GetCriticalPendingCountAsync(criticalThreshold);
            var rejectedToday = await _repository.GetRejectedTodayCountAsync();

            return new AdminDashboardSummaryDto
            {
                TotalPending = statusCounts.GetValueOrDefault("PENDING_APPROVAL", 0),
                TotalApproved = statusCounts.GetValueOrDefault("APPROVED", 0),
                TotalRejected = statusCounts.GetValueOrDefault("REJECTED", 0),
                TotalDraft = statusCounts.GetValueOrDefault("DRAFT", 0),
                TotalSubmitted = statusCounts.GetValueOrDefault("SUBMITTED", 0),
                TotalActive = statusCounts.GetValueOrDefault("ACTIVE", 0),
                ApprovedToday = 0, // Will be populated by the caller's userId context if needed
                RejectedToday = rejectedToday,
                CriticalPending = criticalPending
            };
        }

        public async Task<IEnumerable<FDLandingDto>> GetAdminApprovalListAsync(string? statusFilter)
        {
            return await _repository.GetAdminApprovalListAsync(statusFilter);
        }

        public async Task<AdminApprovalDetailDto?> GetAdminApprovalDetailAsync(long fdId)
        {
            var fd = await _repository.GetByIdAsync(fdId);
            if (fd == null) return null;

            var interest = await _interestRepository.GetByFdIdAsync(fdId);
            var cashFlows = await _cashFlowRepository.GetByFdIdAsync(fdId);
            var approvalHistory = await _repository.GetApprovalHistoryAsync(fdId);

            var createdByName = fd.CreatedBy.HasValue
                ? await _repository.GetUserNameAsync(fd.CreatedBy.Value)
                : "System";

            var modifiedByName = fd.ModifiedBy.HasValue
                ? await _repository.GetUserNameAsync(fd.ModifiedBy.Value)
                : "";

            // Build interest DTO
            AdminInterestDto? interestDto = null;
            if (interest != null)
            {
                interestDto = new AdminInterestDto
                {
                    FdInterestId = interest.FdInterestId,
                    InterestRateType = interest.InterestRateType,
                    InterestRate = interest.InterestRate,
                    BenchmarkId = interest.BenchmarkId,
                    BenchmarkName = interest.BenchmarkName,
                    BenchmarkRate = interest.BenchmarkRate,
                    Margin = interest.Margin,
                    InterestFrequency = interest.InterestFrequency,
                    CompoundingFrequency = interest.CompoundingFrequency,
                    IsCompounding = interest.IsCompounding,
                    CalculationBasis = interest.CalculationBasis,
                    PaymentConvention = interest.PaymentConvention,
                    CreatedDate = interest.CreatedDate
                };
            }

            // Build cash flow DTOs
            var cashFlowDtos = cashFlows
                .OrderBy(c => c.EndDate)
                .ThenBy(c => c.Event)
                .Select(cf => new AdminCashFlowDto
                {
                    CashFlowId = cf.CashFlowId,
                    Event = cf.Event,
                    StartDate = cf.StartDate,
                    EndDate = cf.EndDate,
                    Days = cf.Days,
                    InterestRate = cf.InterestRate,
                    OpeningBalance = cf.OpeningBalance,
                    InterestAmount = cf.InterestAmount,
                    ClosingBalance = cf.ClosingBalance,
                    CashFlowAmount = cf.CashFlowAmount,
                    Direction = cf.Direction,
                    CurrencyCode = cf.CurrencyCode,
                    Status = cf.Status,
                    ReferenceNo = cf.ReferenceNo,
                    CreatedDate = cf.CreatedDate
                }).ToList();

            // Build approval history DTOs
            var historyDtos = new List<AdminApprovalHistoryEntryDto>();
            foreach (var h in approvalHistory)
            {
                var actionByName = await _repository.GetUserNameAsync(h.ActionBy);
                historyDtos.Add(new AdminApprovalHistoryEntryDto
                {
                    Id = h.Id,
                    Action = h.Action,
                    FromStatus = h.FromStatus,
                    ToStatus = h.ToStatus,
                    ActionByUserId = h.ActionBy,
                    ActionByName = actionByName,
                    ActionDate = h.ActionDate,
                    Comments = h.Comments,
                    OldValues = h.OldValues,
                    NewValues = h.NewValues
                });
            }

            // Cash flow summary
            bool isCompounding = interest?.IsCompounding ?? false;
            var maturityRow = cashFlows.FirstOrDefault(c => c.Event == "Maturity");
            decimal maturityAmount = maturityRow?.CashFlowAmount ?? fd.PrincipalAmount;
            decimal totalInterest = isCompounding
                ? Math.Round(maturityAmount - fd.PrincipalAmount, 2, MidpointRounding.AwayFromZero)
                : cashFlows.Where(c => c.Event == "Interest").Sum(c => c.InterestAmount);

            int totalDays = (fd.EndDate.Date - fd.StartDate.Date).Days;

            return new AdminApprovalDetailDto
            {
                FdId = fd.FdId,
                FdReferenceNo = fd.FdReferenceNo,
                EntityId = fd.EntityId,
                EntityName = "", // resolved by frontend or a join
                CounterpartyId = fd.CounterpartyId,
                CounterPartyName = "", // resolved by frontend or a join
                CurrencyCode = fd.CurrencyCode,
                PrincipalAmount = fd.PrincipalAmount,
                StartDate = fd.StartDate,
                EndDate = fd.EndDate,
                SettlementDate = fd.SettlementDate,
                Status = fd.Status,
                Remarks = fd.Remarks,
                CreatedByUserId = fd.CreatedBy,
                CreatedByName = createdByName,
                CreatedDate = fd.CreatedDate,
                ModifiedByUserId = fd.ModifiedBy,
                ModifiedByName = modifiedByName,
                ModifiedDate = fd.ModifiedDate,
                Interest = interestDto,
                CashFlows = cashFlowDtos,
                TotalPrincipal = fd.PrincipalAmount,
                TotalInterest = totalInterest,
                MaturityAmount = maturityAmount,
                TotalTenorDays = totalDays,
                ApprovalHistory = historyDtos
            };
        }
    }
}
