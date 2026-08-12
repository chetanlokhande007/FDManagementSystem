namespace FinTrustFDManager.Model.DTOs.Currency
{
    public class CurrencyDto
    {
        public int CurrencyId { get; set; }

        public string CurrencyCode { get; set; } = string.Empty;

        public string CurrencyName { get; set; } = string.Empty;

        public string? Symbol { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; }
    }
}
