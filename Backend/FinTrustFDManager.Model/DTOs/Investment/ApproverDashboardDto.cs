using System;

namespace FinTrustFDManager.Model.DTOs.Investment
{
    public class ApproverDashboardDto
    {
        /// <summary>
        /// Number of FD records with status PENDING_APPROVAL.
        /// </summary>
        public int TotalPending { get; set; }

        /// <summary>
        /// Number of pending FDs with principal amount exceeding the critical threshold.
        /// The threshold is sourced from configuration (AppSettings:CriticalApprovalThreshold).
        /// If not configured, defaults to 10,000,000 (1 Crore INR).
        /// </summary>
        public int CriticalPending { get; set; }

        /// <summary>
        /// Number of approvals performed by the currently logged-in approver today.
        /// </summary>
        public int ApprovedToday { get; set; }
    }
}
