using FinTrustFDManager.Model.Entities.Investment;
using System.Collections.Generic;

namespace FinTrustFDManager.BAL.DTOs
{
    public class FDCashFlowSummaryDto
    {
        public long FdId { get; set; }
        public string FdReferenceNo { get; set; } = string.Empty;
        public decimal PrincipalAmount { get; set; }
        public decimal InterestRate { get; set; }
        public string InterestRateType { get; set; } = "FIXED";
        public string InterestFrequency { get; set; } = string.Empty;
        public string CompoundingFrequency { get; set; } = "Not Applicable";
        public bool IsCompounding { get; set; }
        public string CalculationBasis { get; set; } = "ACTUAL_365";
        public int TotalTenorDays { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal MaturityAmount { get; set; }
        public List<FDCashFlowDto> Schedule { get; set; } = new();
    }
}