using System;

namespace FinTrustFDManager.Model.Entities.Investment
{
    public class FDCashFlow
    {
        public long CashFlowId { get; set; }

        public long FdId { get; set; }

        public DateTime CashFlowDate { get; set; }

        public string CashFlowType { get; set; } = string.Empty;

        public string Direction { get; set; } = string.Empty;

        public int Days { get; set; }

        public decimal OpeningBalance { get; set; }

        public decimal ClosingBalance { get; set; }

        public decimal PrincipalAmount { get; set; }

        public decimal GrossInterest { get; set; }

        public decimal TdsAmount { get; set; }

        public decimal NetInterest { get; set; }

        public decimal TotalAmount { get; set; }

        public string CurrencyCode { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string? ReferenceNo { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}