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
        
        Task<FDLandingDto?> GetDetailByIdAsync(long id);

        Task<FDIdentification> CreateAsync(CreateFDIdentificationDto dto, long userId);

        Task<FDIdentification?> UpdateAsync(long id, UpdateFDIdentificationDto dto, long userId);

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
        /// Returns a dictionary of FD status → count, used by the approver dashboard.
        /// </summary>
        Task<Dictionary<string, int>> GetStatusCountsAsync();

        /// <summary>
        /// Returns FDs filtered by status for the approver list view.
        /// </summary>
        Task<IEnumerable<FDLandingDto>> GetFilteredAsync(string? status);

        /// <summary>
        /// Returns an FD to the creator for changes (PENDING_FD_ADMIN → RETURNED_TO_CREATOR).
        /// </summary>
        Task<bool> ReturnToCreatorAsync(long fdId, long approverUserId, string comments);
    }
}