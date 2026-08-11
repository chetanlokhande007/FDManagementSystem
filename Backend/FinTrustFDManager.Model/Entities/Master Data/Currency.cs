using FinTrustFDManager.Model.Common;
using FinTrustFDManager.Model.Entities.MasterData;
using System.ComponentModel.DataAnnotations;

namespace FinTrustFDManager.Model.Entities
{
    public class Currency : BaseEntity
    {
        [Key]
        public int CurrencyId { get; set; }

        [Required]
        [MaxLength(10)]
        public string CurrencyCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string CurrencyName { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? Symbol { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }


    }
}