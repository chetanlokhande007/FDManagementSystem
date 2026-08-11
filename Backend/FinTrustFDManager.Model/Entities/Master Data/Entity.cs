using FinTrustFDManager.Model.Common;
using FinTrustFDManager.Model.Entities.MasterData;
using System.ComponentModel.DataAnnotations;

namespace FinTrustFDManager.Model.Entities
{
    public class Entity : BaseEntity
    {
        [Key]
        public int EntityId { get; set; }

        [Required]
        [MaxLength(20)]
        public string EntityCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string EntityName { get; set; } = string.Empty;

        [Required]
        public int CountryId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        // Navigation property
        public Country? Country { get; set; }

        // Navigation property
        public ICollection<BankAccount> BankAccounts { get; set; }
            = new List<BankAccount>();
    }
}