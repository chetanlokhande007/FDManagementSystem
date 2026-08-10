using System;

namespace FinTrustFDManager.Model.Entities.MasterData
{
    public class BankAccount
    {
        public int Id { get; set; }
        public int BankId { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public int CurrencyId { get; set; }
        public bool IsActive { get; set; } = true;
        public Bank Bank { get; set; } = null!;
        public Currency Currency { get; set; } = null!;
    }
}
