namespace FinTrustFDManager.Model.DTOs.Entity
{
    public class EntityDto
    {
        public int EntityId { get; set; }

        public string EntityCode { get; set; } = string.Empty;

        public string EntityName { get; set; } = string.Empty;

        public int CountryId { get; set; }

        public string? CountryName { get; set; }

        public string? Description { get; set; }
    }
}
