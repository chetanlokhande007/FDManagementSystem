namespace FinTrustFDManager.Model.DTOs.InterestFrequency
{
    public class InterestFrequencyDto
    {
        public int Id { get; set; }
        public string FrequencyName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
