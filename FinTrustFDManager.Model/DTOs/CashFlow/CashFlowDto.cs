using System;

namespace FinTrustFDManager.Model.DTOs.CashFlow
{
    public class CashFlowDto
    {
        public int CashFlowId { get; set; }
        public int InvestmentId { get; set; }
        public DateTime CashFlowDate { get; set; }
        public string CashFlowType { get; set; } = string.Empty;
        public decimal PrincipalAmount { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsPaid { get; set; }
        public DateTime? PaidDate { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
