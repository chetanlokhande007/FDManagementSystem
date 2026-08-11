namespace FinTrustFDManager.Model.DTOs.Bank
{
    public class BankDto
    {
        public int BankId { get; set; }

        public string BankCode { get; set; } = string.Empty;

        public string BankName { get; set; } = string.Empty;

        public int CountryId { get; set; }

        public string? CountryName { get; set; }

        public string? SwiftCode { get; set; }

        public string? Description { get; set; }
    }
}
