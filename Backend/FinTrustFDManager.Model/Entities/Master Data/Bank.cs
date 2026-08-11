using FinTrustFDManager.Model.Common;
using FinTrustFDManager.Model.Entities.MasterData;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        // Foreign Key
        [Required]
        public int CountryId { get; set; }

        // Navigation Property
        [ForeignKey(nameof(CountryId))]
        public Country? Country { get; set; }


    }
}