using FinTrustFDManager.Model.Common;
using FinTrustFDManager.Model.Entities.MasterData;
using FinTrustFDManager.Model.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        // Foreign Key
        [Required]
        public int CountryId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

    

        public int? CreatedById { get; set; }

        public EntityStatus Status { get; set; } = EntityStatus.NonApproved;

        // Navigation property
        [ForeignKey(nameof(CountryId))]
        public Country? Country { get; set; }

    }
}