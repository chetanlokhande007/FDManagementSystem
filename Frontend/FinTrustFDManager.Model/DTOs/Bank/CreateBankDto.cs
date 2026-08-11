using System.ComponentModel.DataAnnotations;

namespace FinTrustFDManager.Model.DTOs.Bank
{
    public class CreateBankDto
    {
        [Required]
        [MaxLength(20)]
        public string BankCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string BankName { get; set; } = string.Empty;

        [Required]
        public int CountryId { get; set; }

        [MaxLength(20)]
        public string? SwiftCode { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
