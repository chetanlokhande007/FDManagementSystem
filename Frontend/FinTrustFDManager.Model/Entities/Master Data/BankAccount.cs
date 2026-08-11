using System.ComponentModel.DataAnnotations;
using FinTrustFDManager.Model.Common;

namespace FinTrustFDManager.Model.Entities.MasterData
{
    public class BankAccount : BaseEntity
    {
        [Key]
        public int Id { get; set; }

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


        public Bank? Bank { get; set; }

        public Currency? Currency { get; set; }
    }
}
