using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinTrustFDManager.BAL.DTOs.FDLanding
{
    public class FDLandingDto
    {
        // FD Identification
        public long FdId { get; set; }
        public string FdReferenceNo { get; set; } = string.Empty;
        public long EntityId { get; set; }
        public long CounterpartyId { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public decimal PrincipalAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime SettlementDate { get; set; }
        public string Status { get; set; } = string.Empty;

        // Interest
        public decimal InterestRate { get; set; }
        public string InterestRateType { get; set; } = string.Empty;
        public string InterestFrequency { get; set; } = string.Empty;
        public string CompoundingFrequency { get; set; } = string.Empty;
        public string CalculationBasis { get; set; } = string.Empty;

        // Cash Flow Summary
        public decimal TotalPrincipal { get; set; }
        public decimal TotalGrossInterest { get; set; }
        public decimal TotalTds { get; set; }
        public decimal TotalNetInterest { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
