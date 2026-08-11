namespace FinTrustFDManager.Model.DTOs.BankAccount
{
    public class BankAccountDto
    {
        public int Id { get; set; }

        public int BankId { get; set; }

        public string? BankName { get; set; }

        public string AccountNumber { get; set; } = string.Empty;

        public string AccountName { get; set; } = string.Empty;

        public int CurrencyId { get; set; }

        public string? CurrencyName { get; set; }

        public bool IsActive { get; set; }
    }
}
