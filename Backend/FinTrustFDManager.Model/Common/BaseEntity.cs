using System.ComponentModel.DataAnnotations;

namespace FinTrustFDManager.Model.Common
{
    public abstract class BaseEntity
    {
        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        [StringLength(100)]
        public string? ModifiedBy { get; set; }
    }
}