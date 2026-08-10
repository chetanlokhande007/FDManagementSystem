using FinTrustFDManager.Model.Common;
using FinTrustFDManager.Model.Entities.MasterData;
using System.ComponentModel.DataAnnotations;

namespace FinTrustFDManager.Model.Entities
{
    public class Bank : BaseEntity
    {
        [Key]
        public int BankId { get; set; }

        [Required]
        [MaxLength(20)]
        public string BankCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string BankName { get; set; } = string.Empty;

        public int CountryId { get; set; }

        public string? SwiftCode { get; set; }

        public string? Description { get; set; }

        public Country? Country { get; set; }

        public ICollection<BankAccount>? BankAccounts { get; set; }
    }
}