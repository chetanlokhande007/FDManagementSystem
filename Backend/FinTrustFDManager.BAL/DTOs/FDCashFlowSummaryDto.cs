using FinTrustFDManager.Model.Entities.Investment;
using System.Collections.Generic;

namespace FinTrustFDManager.BAL.DTOs
{
    public class FDCashFlowSummaryDto
    {
        public long FdId { get; set; }
        public decimal PrincipalAmount { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal MaturityAmount { get; set; }
        public List<FDCashFlowDto> CashFlows { get; set; } = [];
    }
}