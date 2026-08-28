using System;
using System.ComponentModel.DataAnnotations;

namespace FinTrustFDManager.Model.Entities
{
    public class FDApprovalHistory
    {
        [Key]
        public long Id { get; set; }

        public long FdId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? FromStatus { get; set; }

        [MaxLength(50)]
        public string? ToStatus { get; set; }

        public long ActionBy { get; set; }

        public DateTime ActionDate { get; set; } = DateTime.UtcNow;

        [MaxLength(1000)]
        public string? Comments { get; set; }

        [MaxLength(4000)]
        public string? OldValues { get; set; }

        [MaxLength(4000)]
        public string? NewValues { get; set; }
    }
}
