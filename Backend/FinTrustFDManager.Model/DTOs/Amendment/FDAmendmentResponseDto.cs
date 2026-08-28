using System;

namespace FinTrustFDManager.Model.DTOs.Amendment
{
    public class FDAmendmentResponseDto
    {
        public long AmendmentId { get; set; }
        public long FdId { get; set; }
        public string FdReferenceNo { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string? RequestedValues { get; set; }
        public string? OriginalValues { get; set; }
        public string? RequestedBy { get; set; }
        public DateTime RequestedDate { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? RejectedBy { get; set; }
        public DateTime? RejectedDate { get; set; }
        public string? ApprovalComments { get; set; }
        public string? RejectionComments { get; set; }
    }
}
