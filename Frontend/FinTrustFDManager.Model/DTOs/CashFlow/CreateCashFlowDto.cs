using System;
using System.ComponentModel.DataAnnotations;

namespace FinTrustFDManager.Model.DTOs.CashFlow
{
    public class CreateCashFlowDto
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
    }
}
