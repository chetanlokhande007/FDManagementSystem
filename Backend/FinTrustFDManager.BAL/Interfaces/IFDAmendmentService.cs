using FinTrustFDManager.Model.DTOs.Amendment;
using FinTrustFDManager.Model.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinTrustFDManager.BAL.Interfaces
{
    public interface IFDAmendmentService
    {
        /// <summary>
        /// Request an amendment to an APPROVED/ACTIVE FD.
        /// Creates the amendment record without modifying the original FD.
        /// </summary>
        Task<FDAmendment> RequestAmendmentAsync(long fdId, FDAmendmentRequestDto request, long requestedBy);

        /// <summary>
        /// Approve a pending amendment. Applies changes to FD, Interest, and regenerates cashflows.
        /// Enforces Maker-Checker: requestor cannot approve their own amendment.
        /// </summary>
        Task<bool> ApproveAmendmentAsync(long fdId, long amendmentId, long approverUserId, string? comments = null);

        /// <summary>
        /// Reject a pending amendment. Original FD remains unchanged.
        /// Enforces Maker-Checker.
        /// </summary>
        Task<bool> RejectAmendmentAsync(long fdId, long amendmentId, long approverUserId, string comments);

        /// <summary>
        /// Get all amendments for an FD.
        /// </summary>
        Task<IEnumerable<FDAmendment>> GetAmendmentsAsync(long fdId);

        /// <summary>
        /// Get a specific amendment by ID.
        /// </summary>
        Task<FDAmendment?> GetAmendmentByIdAsync(long amendmentId);
    }
}
