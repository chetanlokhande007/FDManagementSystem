using System.ComponentModel.DataAnnotations;

namespace FinTrustFDManager.Model.DTOs.CounterParty
{
    public class UpdateCounterPartyDto
    {
        [Required]
        [MaxLength(20)]
        public string CounterPartyCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string CounterPartyName { get; set; } = string.Empty;

        [Required]
        public int CountryId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; }
    }
}
