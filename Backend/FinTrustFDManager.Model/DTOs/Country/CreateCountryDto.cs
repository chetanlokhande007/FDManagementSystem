using System.ComponentModel.DataAnnotations;

namespace FinTrustFDManager.Model.DTOs.Country
{
    public class CreateCountryDto
    {
        [Required]
        [MaxLength(10)]
        public string CountryCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string CountryName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
