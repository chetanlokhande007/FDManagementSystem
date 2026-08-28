using System.ComponentModel.DataAnnotations;

namespace FinTrustFDManager.Model.DTOs.Amendment
{
    public class FDAmendmentRequestDto
    {
        [Required(ErrorMessage = "Reason is required.")]
        [MinLength(5, ErrorMessage = "Reason must be at least 5 characters.")]
        [MaxLength(2000)]
        public string Reason { get; set; } = string.Empty;

        // Financial fields that can be amended (null = no change requested)
        public decimal? PrincipalAmount { get; set; }
        public string? CurrencyCode { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? SettlementDate { get; set; }
        public long? CounterpartyId { get; set; }
        public long? EntityId { get; set; }

        // Interest fields
        public string? InterestRateType { get; set; }
        public decimal? InterestRate { get; set; }
        public int? BenchmarkId { get; set; }
        public decimal? Margin { get; set; }
        public string? InterestFrequency { get; set; }
        public bool? IsCompounding { get; set; }
        public string? CompoundingFrequency { get; set; }
        public string? CalculationBasis { get; set; }
    }
}
