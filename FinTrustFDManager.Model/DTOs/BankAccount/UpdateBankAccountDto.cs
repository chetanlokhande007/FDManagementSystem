using System.ComponentModel.DataAnnotations;

namespace FinTrustFDManager.Model.DTOs.BankAccount
{
    public class UpdateBankAccountDto
    {
        [Required]
        public int BankId { get; set; }

        [Required]
        [MaxLength(50)]
        public string AccountNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string AccountName { get; set; } = string.Empty;

        [Required]
        public int CurrencyId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
