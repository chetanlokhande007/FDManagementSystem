namespace FinTrustFDManager.Model.DTOs.Country
{
    public class CountryDto
    {
        public int CountryId { get; set; }

        public string CountryCode { get; set; } = string.Empty;

        public string CountryName { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
