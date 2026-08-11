using System;

namespace FinTrustFDManager.Model.DTOs.Investment
{
    public class InvestmentDto
    {
        public int Id { get; set; }
        public string InvestmentReferenceNo { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public string? EntityName { get; set; }
        public int CountryId { get; set; }
        public string? CountryName { get; set; }
        public int CurrencyId { get; set; }
        public string? CurrencyName { get; set; }
        public int BankId { get; set; }
        public string? BankName { get; set; }

        public int InterestFrequencyId { get; set; }
        public string? InterestFrequencyName { get; set; }
        public int DayCountConventionId { get; set; }
        public string? DayCountConventionName { get; set; }
        public decimal PrincipalAmount { get; set; }
        public decimal InterestRate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Remarks { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
