using System.ComponentModel.DataAnnotations;

namespace FinTrustFDManager.Model.DTOs.InvestmentApproval
{
    public class UpdateInvestmentApprovalDto
    {
        [Required]
        public int InvestmentId { get; set; }

        [Required]
        public string Action { get; set; } = string.Empty;

        [Required]
        public int ActionBy { get; set; }

        public string? Comments { get; set; }
    }
}
