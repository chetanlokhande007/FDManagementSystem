using FinTrustFDManager.Model.Common;
using System.ComponentModel.DataAnnotations;

namespace FinTrustFDManager.Model.Entities.MasterData
{
    public class Country : BaseEntity
    {
        [Key]
        public int CountryId { get; set; }

        [Required]
        [MaxLength(10)]
        public string CountryCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string CountryName { get; set; } = string.Empty;



        // One Country -> Many Entities
        public ICollection<Entity> Entities { get; set; } = [];

        // One Country -> Many Banks
        public ICollection<Bank> Banks { get; set; } = [];

        // One Country -> Many CounterParties
        public ICollection<CounterParty> CounterParties { get; set; } = [];
    }
}