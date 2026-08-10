using System;
using System.Collections.Generic;
using System.Text;

namespace FinTrustFDManager.Model.Entities.CoreData
{
    public class CashFlow
    {
        public class CashFlow
        {
            public int CashFlowId { get; set; }

            // Investment Reference
            public int InvestmentId { get; set; }

            // Cash Flow Details
            public DateTime CashFlowDate { get; set; }

            // Interest / Principal / Maturity
            public string CashFlowType { get; set; } = string.Empty;

            // Amount Details
            public decimal PrincipalAmount { get; set; }

            public decimal InterestAmount { get; set; }

            public decimal TotalAmount { get; set; }

            // Payment Status
            public string Status { get; set; } = "Pending";

            public bool IsPaid { get; set; } = false;

            public DateTime? PaidDate { get; set; }

            // Audit Fields
            public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

            // Navigation Property
            public Investment? Investment { get; set; }
        }
    }
}
