using System.ComponentModel.DataAnnotations;
using FinTrustFDManager.Model.Common;

namespace FinTrustFDManager.Model.Entities
{
    public class CounterParty : BaseEntity
    {
        [Key]
        public int CounterPartyId { get; set; }

        [Required]
        [MaxLength(20)]
        public string CounterPartyCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string CounterPartyName { get; set; } = string.Empty;

        public int CountryId { get; set; }

        public string? Description { get; set; }

        public Country? Country { get; set; }
    }
}