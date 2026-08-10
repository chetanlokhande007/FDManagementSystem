using System.ComponentModel.DataAnnotations;

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

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
