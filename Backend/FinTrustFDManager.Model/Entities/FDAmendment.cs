using System;
using System.ComponentModel.DataAnnotations;

namespace FinTrustFDManager.Model.Entities
{
    public class FDAmendment
    {
        [Key]
        public long AmendmentId { get; set; }

        public long FdId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "PENDING_APPROVAL";

        [Required]
        [MaxLength(2000)]
        public string Reason { get; set; } = string.Empty;

        // Requested values (JSON serialized financial changes)
        [MaxLength(4000)]
        public string? RequestedValues { get; set; }

        // Original values at time of request (JSON serialized)
        [MaxLength(4000)]
        public string? OriginalValues { get; set; }

        public long RequestedBy { get; set; }

        public DateTime RequestedDate { get; set; } = DateTime.UtcNow;

        public long? ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }

        public long? RejectedBy { get; set; }

        public DateTime? RejectedDate { get; set; }

        [MaxLength(1000)]
        public string? ApprovalComments { get; set; }

        [MaxLength(1000)]
        public string? RejectionComments { get; set; }
    }
}
