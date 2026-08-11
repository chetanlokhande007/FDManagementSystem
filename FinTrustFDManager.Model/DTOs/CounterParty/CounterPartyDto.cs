namespace FinTrustFDManager.Model.DTOs.CounterParty
{
    public class CounterPartyDto
    {
        public int CounterPartyId { get; set; }

        public string CounterPartyCode { get; set; } = string.Empty;

        public string CounterPartyName { get; set; } = string.Empty;

        public int CountryId { get; set; }

        public string? CountryName { get; set; }

        public string? Description { get; set; }
    }
}
