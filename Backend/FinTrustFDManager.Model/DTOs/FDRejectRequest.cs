using System.ComponentModel.DataAnnotations;

namespace FinTrustFDManager.Model.DTOs
{
    public class FDRejectRequest
    {
        [Required(ErrorMessage = "Rejection reason is required.")]
        [MinLength(5, ErrorMessage = "Rejection reason must be at least 5 characters.")]
        [MaxLength(1000)]
        public string Comments { get; set; } = string.Empty;
    }
}
