using System.ComponentModel.DataAnnotations;

namespace FinTrustFDManager.Model.DTOs.InterestFrequency
{
    public class CreateInterestFrequencyDto
    {
        [Required]
        [MaxLength(50)]
        public string FrequencyName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
