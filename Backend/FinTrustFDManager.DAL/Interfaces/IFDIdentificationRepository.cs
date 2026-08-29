using FinTrustFDManager.Model.DTOs.Investment;
using FinTrustFDManager.Model.Entities;
using FinTrustFDManager.Model.Entities.Investment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinTrustFDManager.DAL.Interfaces
{
    public interface IFDIdentificationRepository
    {
        Task<IEnumerable<FDIdentification>> GetAllAsync();

        Task<FDIdentification?> GetByIdAsync(long id);

        Task<FDIdentification> AddAsync(FDIdentification model);

        Task<FDIdentification?> UpdateAsync(FDIdentification model);
        Task<FDIdentification?> GetLastAsync();
        Task<bool> DeleteAsync(long id);

        /// <summary>
        /// Returns FD landing data in a single optimized query
        /// (joins FDIdentification + FDInterest + aggregated FDCashFlow).
        /// Replaces the N+1 query pattern.
        /// </summary>
        Task<IEnumerable<FDLandingDto>> GetLandingDataAsync();

        /// <summary>
        /// Gets the next FD reference number atomically using a PostgreSQL sequence.
        /// Returns the formatted reference (e.g. FD-0001).
        /// </summary>
        Task<string> GetNextFdReferenceNoAsync();

        /// <summary>
        /// Adds an approval history record.
        /// </summary>
        Task AddApprovalHistoryAsync(FDApprovalHistory history);

        /// <summary>
        /// Gets approval history for an FD.
        /// </summary>
        Task<IEnumerable<FDApprovalHistory>> GetApprovalHistoryAsync(long fdId);

        /// <summary>
        /// Counts FDs with PENDING_APPROVAL status.
        /// </summary>
        Task<int> GetPendingCountAsync();

        /// <summary>
        /// Gets FD landing data for FDs with PENDING_APPROVAL status,
        /// joined with entity and counterparty names, for the Approver Dashboard.
        /// </summary>
        Task<IEnumerable<FDLandingDto>> GetPendingApprovalsAsync();

        /// <summary>
        /// Counts FDs with PENDING_APPROVAL status whose principal amount
        /// exceeds the given critical threshold.
        /// </summary>
        Task<int> GetCriticalPendingCountAsync(decimal criticalThreshold);

        /// <summary>
        /// Counts approval actions (APPROVE) performed by the given user today.
        /// </summary>
        Task<int> GetApprovedTodayCountAsync(long approverUserId);

        /// <summary>
        /// Gets the number of FDs in each status for admin dashboard summary.
        /// </summary>
        Task<Dictionary<string, int>> GetStatusCountsAsync();

        /// <summary>
        /// Counts rejection actions performed today.
        /// </summary>
        Task<int> GetRejectedTodayCountAsync();

        /// <summary>
        /// Gets all FDs for the admin approval list, with optional status filter,
        /// ordered by CreatedDate descending.
        /// </summary>
        Task<IEnumerable<FDLandingDto>> GetAdminApprovalListAsync(string? statusFilter);

        /// <summary>
        /// Resolves a user ID to their full name.
        /// </summary>
        Task<string> GetUserNameAsync(long userId);
    }
}
