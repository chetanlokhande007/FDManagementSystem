using FinTrustFDManager.Model.DTOs.Investment;
using FinTrustFDManager.Model.Entities;
using FinTrustFDManager.Model.Entities.Investment;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinTrustFDManager.BAL.Interfaces
{
    public interface IFDIdentificationService
    {
        Task<IEnumerable<FDIdentification>> GetAllAsync();

        Task<FDIdentification?> GetByIdAsync(long id);

        Task<FDIdentification> CreateAsync(FDIdentification model);

        Task<FDIdentification?> UpdateAsync(long id, FDIdentification model);

        Task<bool> DeleteAsync(long id);

        // Landing Page
        Task<IEnumerable<FDLandingDto>> GetLandingDataAsync();

        /// <summary>
        /// Submit an FD for approval (DRAFT/REJECTED → SUBMITTED → PENDING_APPROVAL).
        /// </summary>
        Task<bool> SubmitAsync(long fdId, long userId);

        /// <summary>
        /// Approve a pending FD (PENDING_APPROVAL → APPROVED).
        /// Enforces maker-checker: the submitter cannot approve their own FD.
        /// </summary>
        Task<bool> ApproveAsync(long fdId, long approverUserId, string? comments = null);

        /// <summary>
        /// Reject a pending FD (PENDING_APPROVAL → REJECTED → DRAFT).
        /// Requires rejection comments.
        /// </summary>
        Task<bool> RejectAsync(long fdId, long approverUserId, string comments);

        /// <summary>
        /// Gets the approval history for an FD.
        /// </summary>
        Task<IEnumerable<FDApprovalHistory>> GetApprovalHistoryAsync(long fdId);

        /// <summary>
        /// Gets pending FD approvals for the Approver Dashboard.
        /// </summary>
        Task<IEnumerable<FDLandingDto>> GetPendingApprovalsAsync();

        /// <summary>
        /// Gets approver dashboard summary statistics.
        /// </summary>
        Task<ApproverDashboardDto> GetApproverDashboardSummaryAsync(long approverUserId);

        /// <summary>
        /// Gets admin-specific dashboard summary with counts across all statuses.
        /// </summary>
        Task<AdminDashboardSummaryDto> GetAdminDashboardSummaryAsync();

        /// <summary>
        /// Gets the admin approval list with optional status filter.
        /// </summary>
        Task<IEnumerable<FDLandingDto>> GetAdminApprovalListAsync(string? statusFilter);

        /// <summary>
        /// Gets comprehensive FD detail for admin review (FD + Interest + CashFlows + History).
        /// </summary>
        Task<AdminApprovalDetailDto?> GetAdminApprovalDetailAsync(long fdId);
    }
}