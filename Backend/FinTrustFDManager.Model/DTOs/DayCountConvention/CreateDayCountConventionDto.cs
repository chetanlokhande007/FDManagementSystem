using System.ComponentModel.DataAnnotations;

namespace FinTrustFDManager.Model.DTOs.DayCountConvention
{
    public class CreateDayCountConventionDto
    {
        [Required]
        [MaxLength(50)]
        public string ConventionName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
