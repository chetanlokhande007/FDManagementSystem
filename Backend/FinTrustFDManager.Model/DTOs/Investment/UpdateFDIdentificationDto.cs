using System;
using System.ComponentModel.DataAnnotations;

namespace FinTrustFDManager.Model.DTOs.Investment
{
    public class UpdateFDIdentificationDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Entity is required.")]
        public int EntityId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Counterparty is required.")]
        public int CounterpartyId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Transaction Currency is required.")]
        public int CurrencyId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Principal Amount must be greater than 0.")]
        public decimal PrincipalAmount { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public DateTime SettlementDate { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }
    }
}
