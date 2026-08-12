using System.ComponentModel.DataAnnotations;

namespace FinTrustFDManager.Model.DTOs.Currency
{
    public class UpdateCurrencyDto
    {
        [Required]
        [MaxLength(10)]
        public string CurrencyCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string CurrencyName { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? Symbol { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
