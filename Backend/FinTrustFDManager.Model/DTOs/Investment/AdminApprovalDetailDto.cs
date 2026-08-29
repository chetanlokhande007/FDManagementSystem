using System;
using System.Collections.Generic;

namespace FinTrustFDManager.Model.DTOs.Investment
{
    /// <summary>
    /// Comprehensive admin review DTO combining FD identification, interest config,
    /// cash flow schedule, and approval history for read-only review.
    /// </summary>
    public class AdminApprovalDetailDto
    {
        // ── FD Identification ──
        public long FdId { get; set; }
        public string FdReferenceNo { get; set; } = string.Empty;
        public long EntityId { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public long CounterpartyId { get; set; }
        public string CounterPartyName { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public decimal PrincipalAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? SettlementDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public long? CreatedByUserId { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public long? ModifiedByUserId { get; set; }
        public string ModifiedByName { get; set; } = string.Empty;
        public DateTime? ModifiedDate { get; set; }

        // ── Interest Configuration ──
        public AdminInterestDto? Interest { get; set; }

        // ── Cash Flow Schedule ──
        public List<AdminCashFlowDto> CashFlows { get; set; } = new();

        // ── Cash Flow Summary ──
        public decimal TotalPrincipal { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal MaturityAmount { get; set; }
        public int TotalTenorDays { get; set; }

        // ── Approval History ──
        public List<AdminApprovalHistoryEntryDto> ApprovalHistory { get; set; } = new();
    }

    public class AdminInterestDto
    {
        public long FdInterestId { get; set; }
        public string InterestRateType { get; set; } = string.Empty;
        public decimal InterestRate { get; set; }
        public int? BenchmarkId { get; set; }
        public string? BenchmarkName { get; set; }
        public decimal? BenchmarkRate { get; set; }
        public decimal? Margin { get; set; }
        public string InterestFrequency { get; set; } = string.Empty;
        public string? CompoundingFrequency { get; set; }
        public bool IsCompounding { get; set; }
        public string CalculationBasis { get; set; } = string.Empty;
        public string? PaymentConvention { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class AdminCashFlowDto
    {
        public long CashFlowId { get; set; }
        public string Event { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Days { get; set; }
        public decimal InterestRate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal CashFlowAmount { get; set; }
        public string Direction { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ReferenceNo { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class AdminApprovalHistoryEntryDto
    {
        public long Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? FromStatus { get; set; }
        public string? ToStatus { get; set; }
        public long ActionByUserId { get; set; }
        public string ActionByName { get; set; } = string.Empty;
        public DateTime ActionDate { get; set; }
        public string? Comments { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
    }

    /// <summary>
    /// Summary statistics for the admin dashboard showing counts across all statuses.
    /// </summary>
    public class AdminDashboardSummaryDto
    {
        public int TotalPending { get; set; }
        public int TotalApproved { get; set; }
        public int TotalRejected { get; set; }
        public int TotalDraft { get; set; }
        public int TotalSubmitted { get; set; }
        public int TotalActive { get; set; }
        public int ApprovedToday { get; set; }
        public int RejectedToday { get; set; }
        public int CriticalPending { get; set; }
    }
}
