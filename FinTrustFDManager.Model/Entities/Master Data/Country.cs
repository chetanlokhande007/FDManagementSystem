using FinTrustFDManager.Model.Common;
using FinTrustFDManager.Model.Entities.MasterData;
using System.ComponentModel.DataAnnotations;

namespace FinTrustFDManager.Model.Entities
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

        [MaxLength(250)]
        public string? Description { get; set; }

        public ICollection<Entity> Entities { get; set; }
            = new List<Entity>();

        public ICollection<Bank> Banks { get; set; }
            = new List<Bank>();

        public ICollection<CounterParty> CounterParties { get; set; }
            = new List<CounterParty>();
    }
}