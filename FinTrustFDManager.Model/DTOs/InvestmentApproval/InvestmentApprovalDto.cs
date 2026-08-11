using System;

namespace FinTrustFDManager.Model.DTOs.InvestmentApproval
{
    public class InvestmentApprovalDto
    {
        public int InvestmentApprovalId { get; set; }
        public int InvestmentId { get; set; }
        public string Action { get; set; } = string.Empty;
        public int ActionBy { get; set; }
        public DateTime ActionDate { get; set; }
        public string? Comments { get; set; }
    }
}
