using System;
using System.Collections.Generic;

namespace FinTrustFDManager.Model.DTOs.Investment
{
    public class FDLandingDto
    {
        // FD Identification
        public long FdId { get; set; }
        public string FdReferenceNo { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public int CounterpartyId { get; set; }
        public string CounterPartyName { get; set; } = string.Empty;
        public int CurrencyId { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public decimal PrincipalAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? SettlementDate { get; set; }
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
