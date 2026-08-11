using System;
using System.ComponentModel.DataAnnotations;

namespace FinTrustFDManager.Model.DTOs.CashFlow
{
    public class UpdateCashFlowDto
    {
        [Required]
        public int InvestmentId { get; set; }

        [Required]
        public DateTime CashFlowDate { get; set; }

        [Required]
        public string CashFlowType { get; set; } = string.Empty;

        [Required]
        public decimal PrincipalAmount { get; set; }

        [Required]
        public decimal InterestAmount { get; set; }

        [Required]
        public decimal TotalAmount { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;

        public bool IsPaid { get; set; }

        public DateTime? PaidDate { get; set; }
    }
}
