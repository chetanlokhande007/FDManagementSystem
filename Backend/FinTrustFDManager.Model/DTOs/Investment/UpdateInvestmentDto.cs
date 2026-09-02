using System;
using System.ComponentModel.DataAnnotations;

namespace FinTrustFDManager.Model.DTOs.Investment
{
    public class UpdateInvestmentDto
    {
        [Required]
        public int EntityId { get; set; }

        [Required]
        public int CountryId { get; set; }

        [Required]
        public int CurrencyId { get; set; }



        [Required]
        public int InterestFrequencyId { get; set; }

        [Required]
        public int DayCountConventionId { get; set; }

        [Required]
        public decimal PrincipalAmount { get; set; }

        [Required]
        public decimal InterestRate { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public string? Remarks { get; set; }
        
        [Required]
        public string Status { get; set; } = string.Empty;

        public string? ModifiedBy { get; set; }
    }
}
