using System.ComponentModel.DataAnnotations;

using FinTrustFDManager.Model.Enums;

namespace FinTrustFDManager.Model.DTOs.Entity
{
    public class CreateEntityDto
    {
        [Required]
        [MaxLength(20)]
        public string EntityCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string EntityName { get; set; } = string.Empty;

        [Required]
        public int CountryId { get; set; }

        public EntityStatus Status { get; set; }
    }
}
