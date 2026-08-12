namespace FinTrustFDManager.Model.DTOs.Country
{
    public class CountryDto
    {
        public int CountryId { get; set; }

        public string CountryCode { get; set; } = string.Empty;

        public string CountryName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? ModifiedDate { get; set; }
    }
}
