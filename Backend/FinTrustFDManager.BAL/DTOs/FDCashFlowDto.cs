using System;

namespace FinTrustFDManager.BAL.DTOs
{
    public class FDCashFlowDto
    {
        public long CashFlowId { get; set; }
        public long FdId { get; set; }
        public string Event { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Days { get; set; }
        public decimal InterestRate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal InterestAmount { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal CashFlowAmount { get; set; }
        public string Direction { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ReferenceNo { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
